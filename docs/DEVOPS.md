# DevOps Guide - Sommerhus Project

This guide covers deployment to Microsoft IIS, database migrations, and CI/CD workflows.

---

## Table of Contents

1. [Environment Overview](#environment-overview)
2. [Database Migrations](#database-migrations)
3. [IIS Deployment](#iis-deployment)
4. [GitHub Actions CI/CD](#github-actions-cicd)
5. [Troubleshooting](#troubleshooting)

---

## Environment Overview

| Environment | Purpose         | Database                   | URL                                        |
| ----------- | --------------- | -------------------------- | ------------------------------------------ |
| Development | Local dev       | SQLite (in-memory or file) | localhost:5183 (API), localhost:5015 (MVC) |
| Testing     | Automated tests | SQLite in-memory           | N/A                                        |
| Production  | Live site       | SQLite file                | vh_mms.smedt.dk                            |

### Configuration Files

- `appsettings.json` - Base configuration (shared)
- `appsettings.Development.json` - Local development overrides
- `appsettings.Production.json` - Production settings (secrets via environment variables)

---

## Database Migrations

### Overview

This project uses **Entity Framework Core** with a **code-first** approach. Migrations track schema changes over time.

### Development Workflow

#### 1. Create a New Migration

When you change domain models, create a migration:

```powershell
# From solution root
dotnet ef migrations add <MigrationName> `
  --project Sommerhus.Repository `
  --startup-project Sommerhus.Api `
  --context AppDbContext
```

**Example:**

```powershell
dotnet ef migrations add AddHouseRating `
  --project Sommerhus.Repository `
  --startup-project Sommerhus.Api
```

#### 2. Review the Migration

Always review the generated migration in `Sommerhus.Repository/Migrations/`:

- Check the `Up()` method for expected changes
- Check the `Down()` method for proper rollback
- Verify no data loss will occur

#### 3. Apply Migration Locally

```powershell
dotnet ef database update `
  --project Sommerhus.Repository `
  --startup-project Sommerhus.Api
```

#### 4. Remove a Bad Migration (if needed)

```powershell
# Remove last migration (only if not applied to production)
dotnet ef migrations remove `
  --project Sommerhus.Repository `
  --startup-project Sommerhus.Api
```

### Production Migration Strategy

#### Option A: Automatic Migration on Startup (Current)

The app applies migrations automatically on startup via `Database.Migrate()`:

```csharp
// In Program.cs or a startup service
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await db.Database.MigrateAsync();
```

**Pros:** Simple, no manual steps  
**Cons:** Risk of failed deployment if migration fails

#### Option B: Manual Migration Before Deploy (Recommended for Production)

1. **Generate SQL script:**

```powershell
dotnet ef migrations script `
  --project Sommerhus.Repository `
  --startup-project Sommerhus.Api `
  --idempotent `
  --output migrations.sql
```

2. **Review the SQL script** for any destructive operations

3. **Apply to production database:**

```powershell
# For SQLite
sqlite3 C:\Data\Sommerhus\api\db\sommerhus.db < migrations.sql

# Or use a SQLite GUI tool like DB Browser for SQLite
```

4. **Deploy the application** (migrations already applied)

#### Option C: Bundle Migrations (Best for CI/CD)

Create a migration bundle executable:

```powershell
dotnet ef migrations bundle `
  --project Sommerhus.Repository `
  --startup-project Sommerhus.Api `
  --output efbundle.exe `
  --self-contained
```

Run on production server:

```powershell
.\efbundle.exe --connection "Data Source=C:\Data\Sommerhus\api\db\sommerhus.db"
```

### Migration Best Practices

1. **Never delete migrations** that have been applied to production
2. **Always backup the database** before applying migrations in production
3. **Test migrations** in a staging environment first
4. **Use idempotent scripts** (`--idempotent`) for production SQL
5. **Handle data migrations** in separate, well-tested migrations

### SQLite-Specific Considerations

SQLite has limited ALTER TABLE support. EF Core handles this by:

1. Creating a new table with the new schema
2. Copying data from the old table
3. Dropping the old table
4. Renaming the new table

**Important:** This means migrations can be slow for large tables and require extra disk space.

---

## IIS Deployment

### Prerequisites

1. **.NET 8 Hosting Bundle** installed on the server
2. **IIS** with URL Rewrite module
3. **Application Pools** configured for "No Managed Code"

### Folder Structure

```
C:\WebServer\
├── api_c1_vh_mms.smedt.dk\
│   └── 1.0.0\
│       └── publish\          # API files
│           ├── wwwroot\      # Static files (persisted)
│           ├── Data\         # Database folder (persisted)
│           └── logs\         # Log files (persisted)
└── mvc_c1_vh_mms.smedt.dk\
    └── 1.0.0\
        └── publish\          # MVC files
            ├── wwwroot\
            │   └── uploads\  # User uploads (persisted)
            └── logs\         # Log files (persisted)
```

### Manual Deployment Steps

1. **Build and publish:**

```powershell
dotnet publish Sommerhus.Api -c Release -o C:\_deploy\Sommerhus.Api
dotnet publish Sommerhus.Mvc -c Release -o C:\_deploy\Sommerhus.Mvc
```

2. **Stop the app pools:**

```powershell
Import-Module WebAdministration
Stop-WebAppPool -Name "SommerhusApiPool"
Stop-WebAppPool -Name "SommerhusMvcPool"
```

3. **Put sites offline:**

```powershell
New-Item -Path "C:\WebServer\api_...\publish\app_offline.htm" -ItemType File -Force
New-Item -Path "C:\WebServer\mvc_...\publish\app_offline.htm" -ItemType File -Force
```

4. **Copy files (excluding persistent directories):**

```powershell
robocopy C:\_deploy\Sommerhus.Api C:\WebServer\api_...\publish /MIR /XD wwwroot Data logs
robocopy C:\_deploy\Sommerhus.Mvc C:\WebServer\mvc_...\publish /MIR /XD wwwroot\uploads Data logs
```

5. **Start app pools:**

```powershell
Start-WebAppPool -Name "SommerhusApiPool"
Start-WebAppPool -Name "SommerhusMvcPool"
```

6. **Bring sites online:**

```powershell
Remove-Item -Path "C:\WebServer\api_...\publish\app_offline.htm"
Remove-Item -Path "C:\WebServer\mvc_...\publish\app_offline.htm"
```

### IIS Configuration

#### Application Pool Settings

| Setting               | Value                                             |
| --------------------- | ------------------------------------------------- |
| .NET CLR Version      | No Managed Code                                   |
| Managed Pipeline Mode | Integrated                                        |
| Start Mode            | AlwaysRunning                                     |
| Idle Time-out         | 0 (disabled)                                      |
| Identity              | ApplicationPoolIdentity or custom service account |

#### web.config (auto-generated, but verify)

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\Sommerhus.Api.dll"
                  stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout"
                  hostingModel="InProcess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
```

---

## GitHub Actions CI/CD

### Workflow Overview

| Workflow         | Trigger        | Purpose                  |
| ---------------- | -------------- | ------------------------ |
| `dotnet.yml`     | push, PR       | Build and test           |
| `deploy-iis.yml` | push to master | Deploy to IIS            |
| `smokeTest.yml`  | manual         | Test runner connectivity |

### Current Issues & Fixes

#### 1. `dotnet.yml` - Build and Test Workflow

**Current state:** Build and test steps are commented out.

**Fixed version:**

```yaml
name: .NET Build & Test

on:
  push:
    branches: [master, main, develop]
  pull_request:
    branches: [master, main]

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "8.0.x"

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --configuration Release --no-restore

      - name: Test
        run: dotnet test --no-build --configuration Release --verbosity normal
```

#### 2. `deploy-iis.yml` - Deployment Workflow

**Current issues:**

- Line 122: Uses `robocopy` directly instead of the `$roboArgs` array
- Missing migration step before deployment
- No health check after deployment

**Recommended additions:**

```yaml
# Add after "Publish MVC" step:
- name: Apply Database Migrations
  shell: pwsh
  run: |
    # Create migration bundle
    dotnet ef migrations bundle `
      --project Sommerhus.Repository `
      --startup-project Sommerhus.Api `
      --output "$env:PUBLISH_DIR\efbundle.exe" `
      --self-contained `
      --force

    # Apply migrations
    & "$env:PUBLISH_DIR\efbundle.exe" --connection "$env:ConnectionStrings__Default"

# Add at the end:
- name: Health Check
  shell: pwsh
  run: |
    Start-Sleep -Seconds 10
    $response = Invoke-WebRequest -Uri "https://api.vh_mms.smedt.dk/health" -UseBasicParsing
    if ($response.StatusCode -ne 200) {
      throw "Health check failed with status $($response.StatusCode)"
    }
    Write-Host "API is healthy!"
```

#### 3. Fix Line 122 Bug

The MVC deploy step has a bug - it uses `robocopy` directly instead of the `$roboArgs` array:

```yaml
# WRONG (current):
$null = robocopy $src $dst /MIR /R:1 /W:1

# CORRECT (should be):
$null = robocopy $roboArgs
```

### Self-Hosted Runner Setup

1. **Download the runner** from GitHub repository Settings → Actions → Runners
2. **Install as Windows service:**

```powershell
.\config.cmd --url https://github.com/YOUR_ORG/Sommerhus_project --token YOUR_TOKEN
.\svc.cmd install
.\svc.cmd start
```

3. **Grant permissions:**
   - Add runner service account to IIS_IUSRS group
   - Grant modify permissions on `C:\WebServer\` and `C:\_deploy\`

---

## Troubleshooting

### Common Issues

#### 1. "Unable to configure HTTPS endpoint" in Production

**Cause:** Missing HTTPS certificate or binding  
**Fix:** Configure IIS HTTPS binding with valid certificate

#### 2. "Access denied" during deployment

**Cause:** Files locked by running application  
**Fix:** Ensure `app_offline.htm` is created before copying, and app pools are stopped

#### 3. Migrations fail with "table already exists"

**Cause:** Migration history out of sync  
**Fix:**

```powershell
# Check migration history
dotnet ef migrations list --project Sommerhus.Repository --startup-project Sommerhus.Api

# If needed, mark migrations as applied without running them
dotnet ef database update <LastGoodMigration> --project Sommerhus.Repository --startup-project Sommerhus.Api
```

#### 4. SQLite "database is locked"

**Cause:** Multiple connections or long-running transaction  
**Fix:** Ensure single writer, use WAL mode:

```csharp
optionsBuilder.UseSqlite(connectionString, o => o.CommandTimeout(60));
```

In connection string:

```
Data Source=sommerhus.db;Mode=ReadWriteCreate;Cache=Shared
```

### Useful Commands

```powershell
# Check which process is locking a file
handle64.exe sommerhus.db

# View IIS application pool status
Get-WebAppPoolState -Name "SommerhusApiPool"

# View recent IIS logs
Get-Content "C:\inetpub\logs\LogFiles\W3SVC1\*.log" -Tail 50

# Test API endpoint
Invoke-RestMethod -Uri "http://localhost:5183/api/public/cities"
```

---

## Environment Variables for Production

Set these on the IIS server (via Environment Variables or web.config):

| Variable                     | Description                 | Example                   |
| ---------------------------- | --------------------------- | ------------------------- |
| `ASPNETCORE_ENVIRONMENT`     | Environment name            | `Production`              |
| `ConnectionStrings__Default` | Database connection         | `Data Source=C:\Data\...` |
| `Jwt__Key`                   | JWT signing key (32+ chars) | `YourSecureKeyHere...`    |
| `Jwt__Issuer`                | Token issuer                | `Sommerhus.Api`           |
| `Jwt__Audience`              | Token audience              | `Sommerhus.Admin`         |

**Security:** Never commit secrets to Git. Use environment variables or Azure Key Vault.

---

## Backup Strategy

### Database Backup

```powershell
# Daily backup script (schedule via Task Scheduler)
$date = Get-Date -Format "yyyy-MM-dd"
$src = "C:\Data\Sommerhus\api\db\sommerhus.db"
$dst = "C:\Backups\Sommerhus\sommerhus_$date.db"

Copy-Item $src $dst
# Keep last 30 days
Get-ChildItem "C:\Backups\Sommerhus\*.db" |
  Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } |
  Remove-Item
```

### Uploaded Files Backup

```powershell
robocopy "C:\WebServer\api_...\publish\wwwroot\uploads" "C:\Backups\Uploads" /MIR
```

---

## House Groups Management

### Overview

House groups are used to organize vacation houses and share season calendars between multiple houses. This section covers the complete workflow for managing house groups and season spans.

### House Groups Structure

```
House Group
├── Properties: Id, Name
├── Season Spans: List of date ranges with season codes
└── Houses: Collection of vacation houses
```

### Season Calendar Management

Season spans define when different seasons (high season, low season, etc.) apply to house groups:

```csharp
public record SeasonSpanDto(
    Guid Id,
    DateOnly StartDate,
    DateOnly EndDate,
    string Code,           // Season code (A, B, C, etc.)
    string? SeasonName,    // Display name
    string? Color          // Visual color coding
);
```

### Admin Interface

#### House Groups Management

- **URL**: `/admin/house-groups`
- **Features**:
  - List all house groups with house counts
  - Create/edit/delete house groups
  - Manage season calendars per group
  - Calendar UI with date pickers

#### House Season Calendar

- **URL**: `/admin/houses/{id}?tab=pricing`
- **Features**:
  - View/edit season spans for house's group
  - Add new season periods with calendar UI
  - Delete existing season spans
  - Only available when house is assigned to a group

### API Endpoints

#### House Groups

```http
GET    /api/admin/house-groups              # List groups
POST   /api/admin/house-groups              # Create group
PUT    /api/admin/house-groups/{id}         # Update group
DELETE /api/admin/house-groups/{id}         # Delete group
```

#### Season Spans (Groups)

```http
POST   /api/admin/house-groups/{id}/calendar     # Add season span
PUT    /api/admin/house-groups/{id}/calendar/{id} # Update season span
DELETE /api/admin/house-groups/{id}/calendar/{id} # Delete season span
```

#### Season Spans (Houses)

```http
POST   /api/admin/houses/{id}/calendar     # Add season span
PUT    /api/admin/houses/{id}/calendar/{id} # Update season span
DELETE /api/admin/houses/{id}/calendar/{id} # Delete season span
```

### Common Workflows

#### Creating a New House Group

1. Navigate to `/admin/house-groups`
2. Click "Opret ny gruppe"
3. Enter group name (e.g., "Vestkysten")
4. Save group
5. Click on group to manage season calendar
6. Add season spans using calendar UI

#### Assigning House to Group

1. Navigate to house details: `/admin/houses/{id}?tab=overview`
2. Edit house and select group from dropdown
3. Save changes
4. House now inherits group's season calendar

#### Managing Season Calendar

1. **Via House Group**: `/admin/house-groups/{id}`
   - Add/edit/delete season spans for entire group
   - Changes affect all houses in group

2. **Via Individual House**: `/admin/houses/{id}?tab=pricing`
   - Same season span management interface
   - Changes apply to group (affects all houses)

### Season Codes Setup

Season codes must be created before they can be used in season spans:

1. Navigate to `/admin/pricing`
2. Create season codes with:
   - **Code**: Short identifier (A, B, C, etc.)
   - **Label**: Display name (Højsæson, Lavsæson, etc.)
   - **Color**: Hex color for UI
   - **Sort Order**: Display order

### Data Synchronization

#### Development Environment

```powershell
# House groups and season spans are stored in SQLite
# Database file: Sommerhus.Api/app_data/sommerhus.db

# Tables involved:
# - HouseGroups
# - SeasonSpans
# - VacationHouses (GroupId foreign key)
```

#### Production Environment

```powershell
# Season calendar changes affect pricing calculations
# Ensure all environments have consistent season codes
# Test season span overlaps before deployment
```

### Troubleshooting

#### Common Issues

1. **LINQ Translation Error**: Fixed in Phase 6 - house groups listing now works
2. **Missing Season Calendar**: House must be assigned to a group
3. **Season Code Not Found**: Create season codes in pricing admin first
4. **Date Validation**: End date must be after start date

#### Debugging Season Calendar Issues

```csharp
// Check house group assignment
var house = await _db.Houses.Include(h => h.Group).FirstOrDefaultAsync(h => h.Id == houseId);
Console.WriteLine($"House Group: {house.Group?.Name ?? "None"}");

// Check season spans for group
var spans = await _db.SeasonSpans.Where(s => s.GroupId == house.GroupId).ToListAsync();
Console.WriteLine($"Season Spans: {spans.Count}");
```

---

## Development Workflows

### Complete Feature Development Workflow

This section provides step-by-step tutorials for implementing new features from frontend to backend.

#### Workflow 1: Adding a New Field (Frontend → Backend)

**Scenario**: Add a "MaxGuests" field to houses with validation and database persistence.

##### Step 1: Domain Model Changes

```csharp
// File: Sommerhus.Domain/Models/VacationHouse.cs
public class VacationHouse
{
    // ... existing properties

    [Required, Range(1, 20)]
    public int MaxGuests { get; set; } = 4;  // NEW PROPERTY
}
```

##### Step 2: Create Database Migration

```powershell
# From solution root
dotnet ef migrations add AddHouseMaxGuests `
  --project Sommerhus.Repository `
  --startup-project Sommerhus.Api `
  --context AppDbContext

# Apply locally
dotnet ef database update `
  --project Sommerhus.Repository `
  --startup-project Sommerhus.Api
```

##### Step 3: Update DTOs

```csharp
// File: Sommerhus.Contracts/Dtos/Admin/Houses/UpsertHouseDto.cs
public record UpsertHouseDto(
    string Title,
    string Address,
    string CityId,
    string Description,
    string Facilities,
    string? GroupId,
    int MaxGuests  // NEW FIELD
);

// File: Sommerhus.Contracts/Dtos/Public/Houses/HouseDetailsDto.cs
public record HouseDetailsDto(
    Guid Id,
    string Title,
    string Address,
    string City,
    string Description,
    string Facilities,
    int MaxGuests,  // NEW FIELD
    // ... other fields
);
```

##### Step 4: Update Service Interfaces

```csharp
// File: Sommerhus.Application/Admin/Houses/IAdminHouseService.cs
public interface IAdminHouseService
{
    Task<ServiceResult<HouseDetailsDto>> CreateAsync(UpsertHouseDto dto, CancellationToken ct);
    Task<ServiceResult<HouseDetailsDto>> UpdateAsync(Guid id, UpsertHouseDto dto, CancellationToken ct);
    // ... existing methods
}
```

##### Step 5: Implement Service Logic

```csharp
// File: Sommerhus.Repository/Admin/Houses/AdminHouseService.cs
public async Task<ServiceResult<HouseDetailsDto>> CreateAsync(UpsertHouseDto dto, CancellationToken ct)
{
    var house = new VacationHouse
    {
        Id = Guid.NewGuid(),
        Title = dto.Title,
        Address = dto.Address,
        CityId = Guid.Parse(dto.CityId),
        Description = dto.Description,
        Facilities = dto.Facilities,
        MaxGuests = dto.MaxGuests,  // NEW MAPPING
        GroupId = string.IsNullOrEmpty(dto.GroupId) ? null : Guid.Parse(dto.GroupId)
    };

    db.Houses.Add(house);
    await db.SaveChangesAsync(ct);

    return ServiceResult<HouseDetailsDto>.Success(MapToDetailsDto(house));
}
```

##### Step 6: Update API Controllers

```csharp
// File: Sommerhus.Api/Controllers/Admin/HousesController.cs
[HttpPost]
public async Task<ActionResult<HouseDetailsDto>> Create([FromBody] UpsertHouseDto dto, CancellationToken ct)
{
    var result = await houseService.CreateAsync(dto, ct);
    return this.FromResult(result);
}

[HttpPut("{id:guid}")]
public async Task<ActionResult<HouseDetailsDto>> Update(Guid id, [FromBody] UpsertHouseDto dto, CancellationToken ct)
{
    var result = await houseService.UpdateAsync(id, dto, ct);
    return this.FromResult(result);
}
```

##### Step 7: Update MVC Frontend

```csharp
// File: Sommerhus.Mvc/Services/SommerhusApi.cs
public async Task<ApiResponse<HouseDetailsDto>> CreateHouseAsync(UpsertHouseDto dto, CancellationToken ct)
{
    return await PostAsync<HouseDetailsDto>("/api/admin/houses", dto, ct);
}

public async Task<ApiResponse<HouseDetailsDto>> UpdateHouseAsync(Guid id, UpsertHouseDto dto, CancellationToken ct)
{
    return await PutAsync<HouseDetailsDto>($"/api/admin/houses/{id}", dto, ct);
}
```

##### Step 8: Update MVC Views

```html
<!-- File: Sommerhus.Mvc/Views/Admin/Houses/Create.cshtml -->
<div class="form-group">
  <label asp-for="MaxGuests" class="control-label"></label>
  <input asp-for="MaxGuests" class="form-control" min="1" max="20" />
  <span asp-validation-for="MaxGuests" class="text-danger"></span>
</div>
```

##### Step 9: Update MVC Controller

```csharp
// File: Sommerhus.Mvc/Controllers/Admin/HousesController.cs
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(UpsertHouseDto dto, CancellationToken ct)
{
    if (!ModelState.IsValid)
    {
        await LoadViewData(ct);
        return View(dto);
    }

    var result = await _api.CreateHouseAsync(dto, ct);
    if (!result.Ok)
    {
        ModelState.AddModelError("", result.Message ?? "Failed to create house");
        await LoadViewData(ct);
        return View(dto);
    }

    TempData["Success"] = "House created successfully";
    return RedirectToAction(nameof(Index));
}
```

##### Step 10: Testing

```powershell
# Run all tests
dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj

# Run specific test
dotnet test --filter "TestMethodName"

# Test API manually
curl -X POST http://localhost:5183/api/admin/houses \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"title":"Test House","maxGuests":6,"address":"Test St","cityId":"..."}'
```

#### Workflow 2: Adding a New Entity with Relationships

**Scenario**: Add "Amenities" (like WiFi, Pool) that can be assigned to multiple houses.

##### Step 1: Domain Models

```csharp
// File: Sommerhus.Domain/Models/Amenity.cs
public class Amenity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    [Required, MaxLength(50)]
    public string Icon { get; set; } = "";  // Font Awesome icon class

    public List<HouseAmenity> HouseAmenities { get; set; } = new();
}

// File: Sommerhus.Domain/Models/HouseAmenity.cs (junction table)
public class HouseAmenity
{
    public Guid HouseId { get; set; }
    public Guid AmenityId { get; set; }

    public VacationHouse House { get; set; } = null!;
    public Amenity Amenity { get; set; } = null!;
}
```

##### Step 2: Update DbContext

```csharp
// File: Sommerhus.Repository/Data/DbContext.cs
public DbSet<Amenity> Amenities => Set<Amenity>();
public DbSet<HouseAmenity> HouseAmenities => Set<HouseAmenity>();

// In OnModelCreating:
b.Entity<Amenity>(e =>
{
    e.Property(x => x.Name).IsRequired().HasMaxLength(100);
    e.Property(x => x.Icon).IsRequired().HasMaxLength(50);
});

b.Entity<HouseAmenity>(e =>
{
    e.HasKey(x => new { x.HouseId, x.AmenityId });
    e.HasOne(x => x.House)
        .WithMany(h => h.HouseAmenities)
        .HasForeignKey(x => x.HouseId)
        .OnDelete(DeleteBehavior.Cascade);
    e.HasOne(x => x.Amenity)
        .WithMany(a => a.HouseAmenities)
        .HasForeignKey(x => x.AmenityId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

##### Step 3: Migration and DTOs (follow Workflow 1 steps)

#### Workflow 3: Refactoring - Extracting Common Logic

**Scenario**: Extract duplicate validation logic into a shared service.

##### Step 1: Identify Duplication

Find repeated validation patterns across controllers/services.

##### Step 2: Create Shared Service

```csharp
// File: Sommerhaus.Application/Common/IValidationService.cs
public interface IValidationService
{
    ServiceResult ValidateHouseData(UpsertHouseDto dto);
    ServiceResult ValidateBookingDates(DateOnly start, DateOnly end);
}

// File: Sommerhaus.Repository/Common/ValidationService.cs
public class ValidationService : IValidationService
{
    public ServiceResult ValidateHouseData(UpsertHouseDto dto)
    {
        var errors = new List<string>();

        if (dto.MaxGuests < 1 || dto.MaxGuests > 20)
            errors.Add("MaxGuests must be between 1 and 20");

        if (string.IsNullOrWhiteSpace(dto.Title))
            errors.Add("Title is required");

        return errors.Any()
            ? ServiceResult.Failure(errors)
            : ServiceResult.Success();
    }
}
```

##### Step 3: Update Controllers to Use Shared Service

```csharp
// In all controllers that need validation
public class HousesController(
    IAdminHouseService houseService,
    IValidationService validationService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<HouseDetailsDto>> Create(
        [FromBody] UpsertHouseDto dto, CancellationToken ct)
    {
        var validation = validationService.ValidateHouseData(dto);
        if (!validation.Ok)
            return BadRequest(validation.Errors);

        var result = await houseService.CreateAsync(dto, ct);
        return this.FromResult(result);
    }
}
```

---

## Data Synchronization Strategies

### Keeping Code, Database, and Uploads Synchronized

This section provides comprehensive strategies for maintaining consistency across development machines and production environments.

#### SQLite Development Synchronization

##### Strategy 1: Database-First with Git (Recommended for Teams)

**Setup:**

```powershell
# 1. Create a shared database template
mkdir -p .database/template
cp sommerhus.db .database/template/sommerhus-template.db

# 2. Add to .gitignore
echo ".database/*.db" >> .gitignore
echo "!/database/template/sommerhus-template.db" >> .gitignore

# 3. Create sync script
# File: scripts/sync-database.ps1
param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("pull","push")]
    [string]$Action = "pull"
)

$TemplateDb = ".database/template/sommerhus-template.db"
$LocalDb = "sommerhus.db"

if ($Action -eq "pull") {
    Write-Host "Pulling database template..."
    if (Test-Path $TemplateDb) {
        Copy-Item $TemplateDb $LocalDb -Force
        Write-Host "Database synchronized from template"
    } else {
        Write-Host "No template database found"
    }
} elseif ($Action -eq "push") {
    Write-Host "Pushing database to template..."
    Copy-Item $LocalDb $TemplateDb -Force
    Write-Host "Database template updated"
}
```

**Workflow:**

```powershell
# When starting work (pull latest template)
.\scripts\sync-database.ps1 -Action pull

# When making schema changes:
# 1. Create migration
dotnet ef migrations add NewFeature `
  --project Sommerhus.Repository `
  --startup-project Sommerhus.Api

# 2. Apply locally
dotnet ef database update

# 3. Push updated template
.\scripts\sync-database.ps1 -Action push

# 4. Commit migration and template
git add .
git commit -m "Add NewFeature - updates database template"
```

##### Strategy 2: Migration-Only Sync (Production-Ready)

**Never commit database files. Use migrations only:**

```powershell
# .gitignore
*.db
*.db-shm
*.db-wal
wwwroot/images/**/*.jpg
wwwroot/images/**/*.png
wwwroot/images/**/*.gif
```

**Setup Scripts:**

```powershell
# File: scripts/init-dev-database.ps1
Write-Host "Initializing development database..."

# Ensure database directory exists
mkdir -p .database

# Create fresh database
dotnet ef database update `
  --project Sommerhus.Repository `
  --startup-project Sommerhus.Api

# Run minimal seeding
dotnet run --project Sommerhus.Api --environment Development --no-launch

Write-Host "Development database initialized"
```

```powershell
# File: scripts/reset-database.ps1
param(
    [Parameter(Mandatory=$false)]
    [switch]$KeepData
)

Write-Host "Resetting database..."

if (-not $KeepData) {
    Remove-Item sommerhus.db -Force -ErrorAction SilentlyContinue
    Remove-Item .database -Recurse -Force -ErrorAction SilentlyContinue
}

dotnet ef database drop `
  --project Sommerhus.Repository `
  --startup-project Sommerhus.Api `
  --force

dotnet ef database update `
  --project Sommerhus.Repository `
  --startup-project Sommerhus.Api

Write-Host "Database reset complete"
```

#### File Upload Synchronization

##### Strategy 1: Shared Storage (Recommended)

**Use cloud storage or network share:**

```csharp
// File: Sommerhus.Api/Infrastructure/Storage/CloudImageStorage.cs
public class CloudImageStorage : IImageStorage
{
    private readonly string storageConnectionString;
    private readonly string containerName;

    public CloudImageStorage(IConfiguration config)
    {
        storageConnectionString = config["Storage:ConnectionString"];
        containerName = config["Storage:ContainerName"] ?? "sommerhus-images";
    }

    public async Task<StoredImage> SaveAsync(ImageCategory category, Guid ownerId, IFormFile file, CancellationToken ct)
    {
        var blobClient = new BlobContainerClient(storageConnectionString, containerName)
            .GetBlobClient($"{category}/{ownerId}/{file.FileName}");

        await blobClient.UploadAsync(file.OpenReadStream(), new BlobHttpHeaders { ContentType = file.ContentType }, ct);

        return new StoredImage(file.FileName, blobClient.Uri.ToString());
    }
}
```

**Configuration:**

```json
// appsettings.Development.json
{
  "Storage": {
    "Provider": "Local",
    "LocalPath": "wwwroot/images"
  }
}

// appsettings.Production.json
{
  "Storage": {
    "Provider": "AzureBlob",
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...",
    "ContainerName": "sommerhus-images"
  }
}
```

##### Strategy 2: Git-Tracked Seed Images

**For development/demo images:**

```powershell
# Directory structure
assets/
├── seed-images/
│   ├── houses/
│   │   ├── sample-house-1/
│   │   │   ├── cover.jpg
│   │   │   ├── gallery-1.jpg
│   │   │   └── gallery-2.jpg
│   │   └── sample-house-2/
│   └── areas/
│       └── blavand/
│           └── hero.jpg
```

```csharp
// File: Sommerhus.Repository/Data/ImageSeeder.cs
public static class ImageSeeder
{
    public static async Task SeedImagesAsync(AppDbContext db, IImageStorage storage)
    {
        // Only seed if no images exist
        if (await db.Images.AnyAsync()) return;

        var sampleHouseId = new Guid("5fb7097c-335c-4d07-b4fd-000004e2d28c");
        var seedPath = Path.Combine("assets", "seed-images", "houses", "sample-house-1");

        if (Directory.Exists(seedPath))
        {
            await CopySeedImagesAsync(storage, ImageCategory.House, sampleHouseId, seedPath);
        }
    }

    private static async Task CopySeedImagesAsync(IImageStorage storage, ImageCategory category, Guid ownerId, string sourcePath)
    {
        foreach (var file in Directory.GetFiles(sourcePath, "*.*"))
        {
            var fileName = Path.GetFileName(file);
            var formFile = new FormFile(new FileStream(file, FileMode.Open), 0, new FileInfo(file).Length, "file", fileName);

            await storage.SaveAsync(category, ownerId, formFile, CancellationToken.None);
            formFile.Dispose();
        }
    }
}
```

#### Multi-Developer Workflow

##### Development Environment Setup Script

```powershell
# File: scripts/setup-dev-environment.ps1
Write-Host "Setting up Sommerhus development environment..."

# 1. Restore packages
dotnet restore Sommerhus_project.sln

# 2. Build solution
dotnet build Sommerhus_project.sln

# 3. Initialize database
& .\scripts\init-dev-database.ps1

# 4. Create directories for uploads
mkdir -p wwwroot/images/houses
mkdir -p wwwroot/images/areas
mkdir -p wwwroot/images/cities

# 5. Copy seed images (if any)
if (Test-Path "assets/seed-images") {
    & .\scripts\sync-seed-images.ps1
}

# 6. Run tests to verify setup
dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj --logger "console;verbosity=minimal"

Write-Host "Development environment setup complete!"
Write-Host "Run 'dotnet run --project Sommerhus.Api' to start the API"
Write-Host "Run 'dotnet run --project Sommerhus.Mvc' to start the MVC app"
```

##### Pre-Commit Hook (Optional)

```powershell
# File: .git/hooks/pre-commit (Git Bash)
#!/bin/sh

# Run tests before commit
dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj --no-build --verbosity minimal
if [ $? -ne 0 ]; then
    echo "Tests failed. Commit aborted."
    exit 1
fi

# Check for uncommitted database changes
if git status --porcelain | grep -q "\.db$"; then
    echo "Database files detected. Please remove *.db files before committing."
    exit 1
fi

echo "Pre-commit checks passed."
exit 0
```

#### Production Synchronization

##### Strategy 1: Automated Backup and Sync

```powershell
# File: scripts/production-sync.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$ProductionPath,

    [Parameter(Mandatory=$false)]
    [switch]$DryRun
)

$LocalDb = "sommerhus.db"
$ProductionDb = Join-Path $ProductionPath "Data\sommerhus.db"
$LocalImages = "wwwroot\images"
$ProductionImages = Join-Path $ProductionPath "wwwroot\images"

Write-Host "Production synchronization:"
Write-Host "Local: $LocalDb"
Write-Host "Production: $ProductionDb"

if ($DryRun) {
    Write-Host "DRY RUN - No changes will be made"
}

# Sync database (local -> production)
if (Test-Path $LocalDb) {
    if (-not $DryRun) {
        # Backup production database first
        $backupPath = "$ProductionDb.backup.$(Get-Date -Format 'yyyyMMdd-HHmmss')"
        Copy-Item $ProductionDb $backupPath -ErrorAction SilentlyContinue

        # Copy local database
        Copy-Item $LocalDb $ProductionDb -Force
        Write-Host "Database synchronized"
    } else {
        Write-Host "Would copy: $LocalDb -> $ProductionDb"
    }
}

# Sync images (local -> production)
if (-not $DryRun) {
    robocopy $LocalImages $ProductionImages /MIR /R:1 /W:1
} else {
    Write-Host "Would sync images: $LocalImages -> $ProductionImages"
}
```

##### Strategy 2: Production-First Workflow

**Never overwrite production. Pull from production instead:**

```powershell
# File: scripts/pull-production-data.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$ProductionPath
)

$ProductionDb = Join-Path $ProductionPath "Data\sommerhus.db"
$ProductionImages = Join-Path $ProductionPath "wwwroot\images"

# Pull production database
if (Test-Path $ProductionDb) {
    Copy-Item $ProductionDb "sommerhus.db" -Force
    Write-Host "Production database pulled"
}

# Pull production images
robocopy $ProductionImages "wwwroot\images" /MIR /R:1 /W:1
Write-Host "Production images pulled"
```

#### SQL Server Synchronization

For SQL Server environments, use these additional strategies:

##### Strategy 1: Shared Development Database

```json
// appsettings.Development.json (shared)
{
  "ConnectionStrings": {
    "Default": "Server=dev-sql-server;Database=Sommerhus_Dev;Integrated Security=true;"
  }
}
```

##### Strategy 2: DACPAC for Schema Sync

```powershell
# Extract schema from production
dotnet ef migrations script `
  --project Sommerhus.Repository `
  --startup-project Sommerhus.Api `
  --idempotent `
  --output production-schema.sql

# Compare and apply to development
# Use SQL Server Data Tools or manual comparison
```

##### Strategy 3: Containerized SQL Server

```yaml
# docker-compose.dev.yml
version: "3.8"
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: Y
      SA_PASSWORD: YourStrongPassword123!
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql
      - ./scripts/init-sql.sql:/docker-entrypoint-initdb.d/init.sql

volumes:
  sqlserver_data:
```

---

## Database & Image Synchronization Guide

This section explains how to keep SQLite database and uploaded images synchronized across multiple development machines.

### The Problem

When developing on multiple machines (e.g., desktop at home, laptop at work):

- **SQLite database** (`sommerhus.db`) is stored locally and not committed to Git
- **Uploaded images** (`wwwroot/images/`) are stored locally and gitignored
- Changes made on one machine don't appear on another

### Recommended Solutions

#### Option 1: Commit Database to Git (Simplest for Solo/Small Teams)

**Pros**: Simple, automatic sync via Git  
**Cons**: Binary file, merge conflicts possible, increases repo size

```powershell
# Remove from .gitignore (edit the file)
# Then add and commit
git add Sommerhus.Api/sommerhus.db
git commit -m "Add development database"
```

**Important**: Only do this for development databases. Never commit production databases with real user data.

#### Option 2: Database Template + Migration Script (Recommended)

Keep a clean seed database as a template:

```powershell
# Create template directory
mkdir -p .dev/db-template

# After seeding a fresh database, copy it as template
cp Sommerhus.Api/sommerhus.db .dev/db-template/sommerhus-template.db

# Add template to Git
git add .dev/db-template/sommerhus-template.db
```

Create a setup script `scripts/setup-dev.ps1`:

```powershell
# scripts/setup-dev.ps1
param([switch]$Force)

$templateDb = ".dev/db-template/sommerhus-template.db"
$targetDb = "Sommerhus.Api/sommerhus.db"

if ((Test-Path $targetDb) -and -not $Force) {
    Write-Host "Database already exists. Use -Force to overwrite."
    return
}

if (Test-Path $templateDb) {
    Copy-Item $templateDb $targetDb -Force
    Write-Host "Database initialized from template"
} else {
    Write-Host "No template found. Running migrations..."
    dotnet ef database update --project Sommerhus.Repository --startup-project Sommerhus.Api
}
```

#### Option 3: Cloud Database for Development

Use a shared cloud database (not SQLite):

```json
// appsettings.Development.json
{
  "ConnectionStrings": {
    "Default": "Server=dev-db.example.com;Database=Sommerhus_Dev;User Id=dev;Password=..."
  }
}
```

**Pros**: Always in sync, no local files  
**Cons**: Requires internet, potential conflicts with concurrent edits

### Image Synchronization

#### Option A: Git LFS for Images (Recommended)

```powershell
# Install Git LFS
git lfs install

# Track image files
git lfs track "*.jpg"
git lfs track "*.png"
git lfs track "*.gif"
git lfs track "*.webp"

# Add .gitattributes
git add .gitattributes
git commit -m "Configure Git LFS for images"

# Now images in wwwroot/images will be tracked via LFS
git add wwwroot/images/
git commit -m "Add development images"
```

#### Option B: Seed Images Directory

Keep sample images in a committed `assets/seed-images/` directory:

```powershell
# Directory structure
assets/
└── seed-images/
    └── houses/
        └── sample-house-id/
            ├── cover.jpg
            └── gallery-1.jpg

# Copy to wwwroot on setup
Copy-Item -Recurse assets/seed-images/* Sommerhus.Api/wwwroot/images/
```

#### Option C: Cloud Storage (Production-Ready)

Use Azure Blob Storage or AWS S3:

```csharp
// Configure in Program.cs based on environment
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IImageStorage, PhysicalImageStorage>();
}
else
{
    builder.Services.AddSingleton<IImageStorage, AzureBlobImageStorage>();
}
```

### Complete Setup Script

Create `scripts/dev-setup.ps1`:

```powershell
#!/usr/bin/env pwsh
# Development environment setup script

param(
    [switch]$ResetDatabase,
    [switch]$ResetImages
)

Write-Host "=== Sommerhus Development Setup ===" -ForegroundColor Cyan

# 1. Restore packages
Write-Host "`n[1/5] Restoring packages..." -ForegroundColor Yellow
dotnet restore Sommerhus_project.sln

# 2. Build solution
Write-Host "`n[2/5] Building solution..." -ForegroundColor Yellow
dotnet build Sommerhus_project.sln --no-restore

# 3. Setup database
Write-Host "`n[3/5] Setting up database..." -ForegroundColor Yellow
$dbPath = "Sommerhus.Api/sommerhus.db"
$templatePath = ".dev/db-template/sommerhus-template.db"

if ($ResetDatabase -or -not (Test-Path $dbPath)) {
    if (Test-Path $templatePath) {
        Copy-Item $templatePath $dbPath -Force
        Write-Host "  Database initialized from template" -ForegroundColor Green
    } else {
        Write-Host "  Running migrations..." -ForegroundColor Yellow
        dotnet ef database update --project Sommerhus.Repository --startup-project Sommerhus.Api
    }
} else {
    Write-Host "  Database exists (use -ResetDatabase to recreate)" -ForegroundColor Gray
}

# 4. Setup images
Write-Host "`n[4/5] Setting up images..." -ForegroundColor Yellow
$imagesPath = "Sommerhus.Api/wwwroot/images"
$seedImagesPath = "assets/seed-images"

if (-not (Test-Path $imagesPath)) {
    New-Item -ItemType Directory -Path $imagesPath -Force | Out-Null
}

if ($ResetImages -and (Test-Path $seedImagesPath)) {
    Copy-Item -Recurse -Force "$seedImagesPath/*" $imagesPath
    Write-Host "  Images copied from seed directory" -ForegroundColor Green
}

# 5. Run tests
Write-Host "`n[5/5] Running tests..." -ForegroundColor Yellow
dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj --no-build

Write-Host "`n=== Setup Complete ===" -ForegroundColor Cyan
Write-Host "Run 'dotnet run --project Sommerhus.Api' to start the API"
Write-Host "Run 'dotnet run --project Sommerhus.Mvc' to start the MVC app"
```

### Workflow for Multiple Developers

1. **Initial Setup** (each developer once):

   ```powershell
   git clone <repo>
   cd Sommerhus_project
   ./scripts/dev-setup.ps1
   ```

2. **After Pulling Changes**:

   ```powershell
   git pull
   dotnet build
   # Run migrations if schema changed
   dotnet ef database update --project Sommerhus.Repository --startup-project Sommerhus.Api
   ```

3. **When Making Database Changes**:

   ```powershell
   # Create migration
   dotnet ef migrations add YourMigrationName --project Sommerhus.Repository --startup-project Sommerhus.Api

   # Apply migration
   dotnet ef database update --project Sommerhus.Repository --startup-project Sommerhus.Api

   # Commit migration files
   git add Sommerhus.Repository/Migrations/
   git commit -m "Add YourMigrationName migration"
   ```

4. **Sharing Seed Data**:
   ```powershell
   # After creating good test data, save as template
   cp Sommerhus.Api/sommerhus.db .dev/db-template/sommerhus-template.db
   git add .dev/db-template/
   git commit -m "Update database template with new seed data"
   ```

### Troubleshooting

#### Database Out of Sync

```powershell
# Reset to template
./scripts/dev-setup.ps1 -ResetDatabase

# Or delete and recreate
Remove-Item Sommerhus.Api/sommerhus.db
dotnet ef database update --project Sommerhus.Repository --startup-project Sommerhus.Api
```

#### Images Missing

```powershell
# If using seed images
./scripts/dev-setup.ps1 -ResetImages

# If using Git LFS, ensure it's installed
git lfs install
git lfs pull
```

#### Migration Conflicts

```powershell
# Check migration history
dotnet ef migrations list --project Sommerhus.Repository --startup-project Sommerhus.Api

# Remove failed migration
dotnet ef migrations remove --project Sommerhus.Repository --startup-project Sommerhus.Api
```

---

## Next Steps

1. [ ] Add health check endpoint to API (`/health`)
2. [ ] Set up monitoring/alerting (Application Insights, Seq, etc.)
3. [ ] Configure SSL certificate auto-renewal
4. [ ] Add staging environment for testing deployments
5. [ ] Implement blue-green deployment for zero-downtime updates
6. [ ] Create automated database backup to cloud storage
7. [ ] Set up image CDN for production uploads

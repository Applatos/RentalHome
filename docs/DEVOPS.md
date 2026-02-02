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

| Environment | Purpose | Database | URL |
|-------------|---------|----------|-----|
| Development | Local dev | SQLite (in-memory or file) | localhost:5183 (API), localhost:5015 (MVC) |
| Testing | Automated tests | SQLite in-memory | N/A |
| Production | Live site | SQLite file | vh_mms.smedt.dk |

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

| Setting | Value |
|---------|-------|
| .NET CLR Version | No Managed Code |
| Managed Pipeline Mode | Integrated |
| Start Mode | AlwaysRunning |
| Idle Time-out | 0 (disabled) |
| Identity | ApplicationPoolIdentity or custom service account |

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

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `dotnet.yml` | push, PR | Build and test |
| `deploy-iis.yml` | push to master | Deploy to IIS |
| `smokeTest.yml` | manual | Test runner connectivity |

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
        dotnet-version: '8.0.x'
    
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

| Variable | Description | Example |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Production` |
| `ConnectionStrings__Default` | Database connection | `Data Source=C:\Data\...` |
| `Jwt__Key` | JWT signing key (32+ chars) | `YourSecureKeyHere...` |
| `Jwt__Issuer` | Token issuer | `Sommerhus.Api` |
| `Jwt__Audience` | Token audience | `Sommerhus.Admin` |

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

## Next Steps

1. [ ] Add health check endpoint to API (`/health`)
2. [ ] Set up monitoring/alerting (Application Insights, Seq, etc.)
3. [ ] Configure SSL certificate auto-renewal
4. [ ] Add staging environment for testing deployments
5. [ ] Implement blue-green deployment for zero-downtime updates

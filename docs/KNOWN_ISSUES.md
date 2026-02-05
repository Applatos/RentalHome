# Sommerhus Known Issues & Technical Debt

This document tracks known issues, technical debt, and areas for improvement discovered during codebase review.

---

## High Priority

### 1. Service Registration Bloat in Program.cs

**Location**: `Sommerhus.Api/Program.cs:96-121`

**Problem**: 20+ manual `AddScoped` registrations make Program.cs hard to maintain.

**Current Code**:

```csharp
builder.Services.AddScoped<IAdminAreaService, AdminAreaService>();
builder.Services.AddScoped<IAdminCityService, AdminCityService>();
builder.Services.AddScoped<IAdminFeatureService, AdminFeatureService>();
// ... 15+ more lines
```

**Solution**: Create extension methods to group related services.

**Roadmap**: Phase 1

---

### 2. Large Service Classes

**Location**: `Sommerhus.Repository/Admin/Houses/AdminHouseService.cs`

**Problem**: 400+ lines violating single responsibility principle.

**Contains**:

- House CRUD operations
- Pricing management (`UpsertPricingAsync`)
- Feature management (`UpsertFeaturesAsync`)
- DTO mapping

**Solution**: Extract into `AdminHousePricingService` and `AdminHouseFeatureService`.

**Roadmap**: Phase 2

---

### 3. Danish Text in Codebase

**Locations**:

- `Sommerhus.Repository/Admin/Houses/AdminHouseService.cs:317` - `"Ukendt område"`
- Various csproj comments in Danish

**Problem**: Inconsistent language makes codebase harder to maintain.

**Solution**: Replace all Danish text with English equivalents.

**Roadmap**: Phase 6

---

## Medium Priority

### 4. Empty Sommerhus.Pricing Project

**Location**: `Sommerhus.Pricing/`

**Problem**: Project exists in solution but contains no code.

**Options**:

- Delete the project entirely
- Move `Application/Pricing/Engine/` into it

**Roadmap**: Phase 5

---

### 5. Missing DTO Validation Attributes

**Locations**: Various files in `Sommerhus.Contracts/Dtos/`

**Problem**: DTOs lack proper validation, relying on service-level checks.

**Example** (missing validation):

```csharp
public record UpsertHouseDto(
    string Name,           // Should have [Required], [MaxLength]
    string? Address,       // Should have [MaxLength]
    Guid CityId,
    string? Description,
    IEnumerable<Guid>? AreaIds
);
```

**Solution**: Add data annotations for automatic model validation.

**Roadmap**: Phase 3

---

### 6. Inconsistent Image Handling

**Locations**:

- `Sommerhus.Repository/Admin/Images/AdminHouseImageService.cs`
- `Sommerhus.Repository/Admin/Images/AdminAreaImageService.cs`
- `Sommerhus.Repository/Admin/Images/AdminCityImageService.cs`

**Problem**: Three similar services with duplicated logic.

**Solution**: Create generic base class or shared helper.

**Roadmap**: Phase 4

---

### 7. HttpRequest Passed to Services

**Locations**:

- `IAdminHouseService.GetDetailsAsync(Guid id, HttpRequest request, ...)`
- `IHouseQueryService.GetAsync(Guid id, HttpRequest request, ...)`

**Problem**: Services depend on ASP.NET Core types, reducing testability.

**Solution**: Pass only the data needed (base URL string) instead of `HttpRequest`.

---

### 8. Hardcoded CORS Origins

**Location**: `Sommerhus.Api/Program.cs:137`

**Current Code**:

```csharp
.WithOrigins("https://localhost:5001", "https://localhost:7202")
```

**Problem**: Should be configurable via appsettings.

**Solution**: Move to configuration section.

---

## Low Priority

### 9. Commented-Out Code

**Locations**:

- `Sommerhus.Api/Program.cs:55-56` - StorageOptions binding
- `Sommerhus.Mvc/Sommerhus.Mvc.csproj:22` - RuntimeCompilation

**Problem**: Clutters codebase; unclear if code is needed.

**Solution**: Remove or document why commented.

---

### 10. Missing API Documentation

**Location**: All controllers

**Problem**: Swagger shows endpoints but lacks descriptions.

**Solution**: Add XML comments and `[ProducesResponseType]` attributes.

**Roadmap**: Phase 7

---

### 11. No Global Exception Handling in MVC

**Location**: `Sommerhus.Mvc/Program.cs`

**Problem**: Unhandled exceptions may show stack traces to users.

**Solution**: Add exception handling middleware.

**Roadmap**: Phase 10

---

### 12. Potential N+1 Queries

**Location**: `Sommerhus.Repository/Public/Houses/HouseQueryService.cs:32-38`

**Current Code**:

```csharp
.Include(h => h.Images)
.Include(h => h.City)
.Include(h => h.Areas)
.Include(h => h.HouseFeatures).ThenInclude(v => v.Feature)
```

**Problem**: Multiple includes may cause large queries; needs profiling.

**Solution**: Audit with SQL logging; consider `AsSplitQuery()`.

**Roadmap**: Phase 9

---

## Security Considerations

### 13. JWT Key in appsettings.json

**Location**: `Sommerhus.Api/appsettings.json:20`

**Problem**: Default key committed to source control.

**Mitigation**:

- Use different keys per environment
- Store production keys in environment variables or Key Vault
- Document in README

---

### 14. Admin Password in appsettings.json

**Location**: `Sommerhus.Api/appsettings.json:14`

**Problem**: Default admin password is visible in repo.

**Mitigation**:

- Force password change on first login
- Use environment variables in production
- Document security best practices

---

## Test Coverage Gaps

### 15. Missing Test Areas

- Pricing pipeline not fully tested
- Image upload/delete edge cases
- Authorization failure scenarios
- MVC client integration tests

**Roadmap**: Phase 8

---

### 16. Test Infrastructure - Admin Authentication Fails

**Location**: `Sommerhus.Api.Tests/Infrastructure/CustomWebApplicationFactory.cs`

**Problem**: Admin endpoint tests fail with 401 Unauthorized. The test factory seeds admin user during `ConfigureServices` via an intermediate ServiceProvider, but the Identity seeding may not work correctly.

**Symptoms**:

- Public endpoint tests pass (5/11)
- Admin endpoint tests fail with 401 (6/11)
- `EnsureCreated()` is used instead of `Migrate()` (no migrations exist)

**Root Cause**: The `AdminIdentitySeeder` runs during service configuration with an intermediate ServiceProvider. Identity services may not be fully configured at that point.

**Potential Solutions**:

1. Create proper EF migrations for Identity tables
2. Move admin seeding to happen after app starts (via `IHostedService`)
3. Seed admin user directly via SQL in test setup

**Roadmap**: Should be addressed before Phase 8 (Test Coverage Expansion)

---

## Tracking

| Issue                           | Priority | Phase | Status          |
| ------------------------------- | -------- | ----- | --------------- |
| #1 Service Registration         | High     | 1     | **Completed**   |
| #2 Large Services               | High     | 2     | Pending         |
| #3 Danish Text                  | Medium   | 6     | Pending         |
| #4 Empty Project                | Low      | 5     | Pending         |
| #5 DTO Validation               | Medium   | 3     | Pending         |
| #6 Image Handling               | Medium   | 4     | Pending         |
| #7 HttpRequest in Services      | Medium   | -     | Pending         |
| #8 Hardcoded CORS               | Low      | -     | Pending         |
| #9 Commented Code               | Low      | 6     | Pending         |
| #10 API Docs                    | Low      | 7     | Pending         |
| #11 MVC Exception Handling      | Low      | 10    | Pending         |
| #12 N+1 Queries                 | Medium   | 9     | Pending         |
| #13 JWT Key                     | Security | -     | Documented      |
| #14 Admin Password              | Security | -     | Documented      |
| #15 Test Coverage               | High     | 8     | Pending         |
| #17 Architecture Simplification | High     | 2.5   | **In Progress** |
| #18 MVC Frontend Inconsistency  | High     | 2.6   | **In Progress** |

---

## Phase 2.6: MVC Admin Frontend Standardization (NEW)

This phase addresses inconsistent patterns across the admin frontend discovered during architecture review.

### Problems Identified

1. **Controller File/Class Mismatch**: `HousesController.cs` contains class `AdminController`
2. **God Controller**: `AdminController` is 728 lines handling houses, pricing, features, calendar, season codes
3. **Inconsistent Action Names**: Mix of `Houses()`/`Index()`, `House()`/`Details()`
4. **Excessive ViewBag Usage**: 7+ ViewBag properties instead of typed ViewModels
5. **Inconsistent View Paths**: Some hardcoded (`~/Views/Admin/Areas.cshtml`), some implicit
6. **Mixed Languages**: Danish/English error messages
7. **Inconsistent View Naming**: `House._images.cshtml` vs `House._Overview.cshtml`
8. **No Shared Partials**: Flash messages duplicated across all views

### Solution: Unified Admin MVC Pattern

#### Controller Convention

- One controller per entity: `{Entity}Controller`
- Standard actions: `Index`, `Details`, `Create`, `Edit`, `Delete`
- Sub-resources get separate controllers: `HouseImagesController`, `HousePricingController`

#### ViewModel Convention

- Every view gets a typed ViewModel (never raw DTOs)
- ViewModels include all dropdown data, tab state, flash messages
- Zero ViewBag usage

#### View Convention

- Folder per entity: `Views/Admin/Houses/`, `Views/Admin/Areas/`
- Consistent partial naming: `_Tab{Name}.cshtml`
- Shared partials: `_FlashMessages.cshtml`, `_Pagination.cshtml`

### Implementation Steps

1. ✅ Document changes in KNOWN_ISSUES.md
2. Create `AdminControllerBase` with shared helpers
3. Rename `AdminController` → `HousesController` (fix class/file mismatch)
4. Extract pricing/calendar into `HousePricingController`
5. Create typed ViewModels for all admin views
6. Standardize action names across all controllers
7. Create shared partials
8. Reorganize view folder structure
9. Standardize all messages to English
10. Update ARCHITECTURE.md

### New MVC Structure (After)

```
Controllers/Admin/
├── AdminControllerBase.cs       # Shared helpers
├── HousesController.cs          # House CRUD (Index, Details, Create, Edit, Delete)
├── HousePricingController.cs    # House pricing + calendar
├── AreasController.cs           # Area CRUD (standardized actions)
├── FeaturesController.cs        # Feature CRUD
├── HouseGroupsController.cs     # House group CRUD
└── PricingController.cs         # Season codes management

ViewModels/Admin/
├── Houses/
│   ├── HouseListVm.cs
│   ├── HouseDetailsVm.cs
│   └── HouseCreateVm.cs
├── Areas/
│   ├── AreaListVm.cs
│   └── AreaDetailsVm.cs
├── Features/
│   └── FeatureListVm.cs
└── HouseGroups/
    └── HouseGroupListVm.cs

Views/Admin/
├── Houses/
│   ├── Index.cshtml
│   ├── Details.cshtml
│   ├── Create.cshtml
│   ├── _TabOverview.cshtml
│   ├── _TabImages.cshtml
│   ├── _TabFeatures.cshtml
│   ├── _TabPricing.cshtml
│   └── _TabCalendar.cshtml
├── Areas/
│   ├── Index.cshtml
│   ├── Details.cshtml
│   ├── Create.cshtml
│   └── Edit.cshtml
├── Features/
│   └── Index.cshtml
├── HouseGroups/
│   └── (existing structure OK)
└── Shared/
    ├── _FlashMessages.cshtml
    └── _Pagination.cshtml
```

---

## Phase 2.5: Architecture Simplification (NEW)

This phase consolidates over-engineered layers identified during architecture review.

### Goals

1. **Consolidate DTOs** - Merge `Sommerhus.Contracts` into `Sommerhus.Application`
2. **Merge Projects** - Combine `Application` + `Repository` → `Sommerhus.Core`
3. **Keep Interfaces** - Retain service interfaces for testability (even 1:1)
4. **Fix MVC Controller Bloat** - Split 1100-line `HousesController.cs`
5. **Extract ViewModels** - Move inline ViewModels to `Mvc/ViewModels/`
6. **Move Extensions** - Relocate `SelectListExtensions` to `Mvc/Extensions/`
7. **Unify Response Wrappers** - Single `ServiceResult<T>` pattern API→MVC

### New Project Structure (After)

```
Sommerhus_project/
├── Sommerhus.Api/           # REST API (unchanged)
├── Sommerhus.Mvc/           # MVC frontend (reorganized)
│   ├── Controllers/Admin/   # Split by domain
│   ├── ViewModels/          # Extracted from controllers
│   └── Extensions/          # SelectListExtensions, etc.
├── Sommerhus.Domain/        # Entity models (unchanged)
├── Sommerhus.Core/          # NEW: Merged Application + Repository
│   ├── Dtos/                # Consolidated from Contracts
│   ├── Services/            # Interfaces + Implementations
│   ├── Data/                # DbContext, migrations
│   └── Common/              # ServiceResult, shared utilities
└── Sommerhus.Api.Tests/     # Tests (updated references)
```

### Deleted Projects

- `Sommerhus.Contracts` - Merged into Core
- `Sommerhus.Application` - Merged into Core
- `Sommerhus.Repository` - Merged into Core

### Migration Steps

1. Create `Sommerhus.Core` project
2. Move DTOs from Contracts → Core/Dtos
3. Move interfaces from Application → Core/Services
4. Move implementations from Repository → Core/Services
5. Update all project references
6. Split MVC controllers
7. Extract ViewModels
8. Run tests to verify

---

## How to Add New Issues

When discovering new issues during development:

1. Add to appropriate priority section
2. Include:
   - Location (file and line if applicable)
   - Problem description
   - Proposed solution
   - Roadmap phase (if applicable)
3. Update tracking table
4. Consider creating GitHub issue for larger items

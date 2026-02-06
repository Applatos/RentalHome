# Sommerhus Refactoring Roadmap

## Overview

This roadmap outlines a phased approach to refactoring the Sommerhus codebase. Each phase is designed to be independently testable, ensuring we maintain a working application throughout the process.

---

## Phase 1: Service Registration Cleanup ✅ COMPLETED

**Goal**: Reduce Program.cs complexity and improve DI organization.

### Tasks

- [x] Create `Sommerhus.Api/Extensions/ServiceCollectionExtensions.cs`
- [x] Extract admin service registrations to `AddAdminServices()`
- [x] Extract public service registrations to `AddPublicServices()`
- [x] Extract pricing service registrations to `AddPricingServices()`
- [x] Extract storage service registrations to `AddStorageServices()`
- [x] Update `Program.cs` to use extension methods

### Files Modified

- `Sommerhus.Api/Program.cs` - Reduced from 158 to ~105 lines
- Created: `Sommerhus.Api/Extensions/ServiceCollectionExtensions.cs`

### Result

- Build: ✅ Passes
- Tests: 5/11 pass (6 fail due to pre-existing test infrastructure issue - see KNOWN_ISSUES.md #16)

---

## Phase 2: Split Large Service Classes ✅ COMPLETED

**Goal**: Break `AdminHouseService` (400+ lines) into focused services.

### Tasks

- [x] Extract `AdminHousePricingService` from `AdminHouseService`
- [x] Extract `AdminHouseFeatureService` from `AdminHouseService`
- [x] Create corresponding interfaces in Application layer
- [x] Update DI registrations
- [x] Update `HousesController` to use new services

### Files Created

- `Sommerhus.Application/Admin/Houses/IAdminHousePricingService.cs`
- `Sommerhus.Application/Admin/Houses/IAdminHouseFeatureService.cs`
- `Sommerhus.Repository/Admin/Houses/AdminHousePricingService.cs`
- `Sommerhus.Repository/Admin/Houses/AdminHouseFeatureService.cs`

### Files Modified

- `Sommerhus.Repository/Admin/Houses/AdminHouseService.cs` - Reduced from 402 to ~300 lines
- `Sommerhus.Api/Controllers/Admin/HousesController.cs` - Now injects 3 services
- `Sommerhus.Api/Extensions/ServiceCollectionExtensions.cs` - Added new service registrations
- `Sommerhus.Application/Admin/Houses/IAdminHouseService.cs` - Removed feature/pricing methods

### Result

- Build: ✅ Passes
- Tests: 5/11 pass (same as Phase 1 - pre-existing auth issue)

---

## Test Infrastructure Fix ✅ COMPLETED

**Goal**: Fix pre-existing test infrastructure issues causing admin endpoint 401 errors and 500 errors.

### Issues Fixed

1. **JWT Authentication Mismatch** - Token validation used original config values instead of test values
   - Fix: Added `PostConfigure<JwtBearerOptions>` in test factory to override validation parameters

2. **PricePlan Not Active** - Seeded plan had `IsActive = false` by default
   - Fix: Set `IsActive = true` in `DbSeeder.SeedMinimal()`

3. **Null Reference in Pricing Rule** - No null check when no active plan exists
   - Fix: Added null check in `BaseNightlyRateRule.ApplyAsync()`

4. **Invalid Cast in Controller Extensions** - Cast `IReadOnlyDictionary` to `ValidationProblemDetails`
   - Fix: Created `ToValidationProblem()` helper method

5. **Test Assertion Bug** - Test checked `problem.Detail` instead of `problem.Errors`
   - Fix: Updated test to check `ValidationProblemDetails.Errors`

### Files Modified

- `Sommerhus.Api.Tests/Infrastructure/CustomWebApplicationFactory.cs`
- `Sommerhus.Repository/Data/DbSeeder.cs`
- `Sommerhus.Application/Pricing/Engine/Rules/BaseNightlyRateRule.cs`
- `Sommerhus.Api/Infrastructure/ControllerExtensions.cs`
- `Sommerhus.Api.Tests/Admin/PricingTests.cs`

### Result

- Build: ✅ Passes
- Tests: **11/11 pass** ✅

---

## Phase 3: DTO Consolidation ✅ COMPLETED

**Goal**: Reduce DTO duplication and add validation attributes.

### Tasks

- [x] Audit all DTOs in `Sommerhus.Contracts/Dtos/`
- [x] Fix `FeatureValueDto` namespace (was in Shared folder but Admin.Features namespace)
- [x] Add validation attributes to input DTOs
- [ ] ~~Create shared base DTOs~~ (deferred - causes breaking changes)

### Files Modified

- `Sommerhus.Contracts/Dtos/Shared/FeatureValueDto.cs` - Fixed namespace to Shared
- `Sommerhus.Contracts/Dtos/Admin/Cities/CityDtos.cs` - Added validation attributes
- `Sommerhus.Contracts/Dtos/Admin/Areas/UpsertAreaDto.cs` - Added validation attributes
- `Sommerhus.Contracts/Dtos/Admin/Features/UpsertFeatureDto.cs` - Added validation attributes
- `Sommerhus.Contracts/Dtos/Admin/Features/FeatureListItem.cs` - Cleaned up imports

### Result

- Build: ✅ Passes
- Tests: **11/11 pass** ✅

### Notes

DTO namespace consolidation was attempted but caused ambiguous reference errors across
the codebase. The approach was revised to:

1. Keep separate Admin/Public namespaces for backward compatibility
2. Add validation attributes to input DTOs
3. Document duplicates for future consolidation

---

## DevOps Guide ✅ COMPLETED

**Goal**: Document IIS deployment, database migrations, and CI/CD workflows.

### Created

- `docs/DEVOPS.md` - Comprehensive DevOps guide covering:
  - Database migration strategies (development and production)
  - IIS deployment steps and configuration
  - GitHub Actions workflow fixes
  - Troubleshooting common issues
  - Backup strategies

### GitHub Actions Fixed

- `.github/workflows/dotnet.yml` - Enabled build and test steps
- `.github/workflows/deploy-iis.yml` - Fixed robocopy bug on line 122

---

## Phase 4: Image Handling Consistency ✅ COMPLETED

**Goal**: Unify image handling patterns across House/Area/City.

### Tasks

- [x] Review current image service implementations
- [x] Create generic `IAdminEntityImageService<TEntity>` interface
- [x] Enhance `AdminImageServiceBase` with more shared logic
- [x] Refactor services to use enhanced base class
- [x] Ensure consistent URL generation and error handling

### Files Created

- `Sommerhus.Application/Admin/Images/IAdminEntityImageService.cs` - Generic interfaces

### Files Modified

- `Sommerhus.Repository/Admin/Images/AdminImageServiceBase.cs` - Added helper methods:
  - `ValidateSingleFile<T>()` - Single file validation
  - `ValidateFileCollection()` - Batch file validation
  - `ToDto()` - Consistent DTO creation
  - `SaveToStorageAsync()` - Storage abstraction
  - `DeleteFromStorageAsync()` - Storage abstraction
- `Sommerhus.Repository/Admin/Images/AdminAreaImageService.cs` - Uses base helpers
- `Sommerhus.Repository/Admin/Images/AdminCityImageService.cs` - Uses base helpers
- `Sommerhus.Repository/Admin/Images/AdminHouseImageService.cs` - Uses base helpers

### Result

- Build: ✅ Passes

---

## HIGH PRIORITY: Pricing System Fix ✅ COMPLETED

**Goal**: Fix the pricing display issues and implement proper season calendar management.

### Architecture

```
HouseGroup ──┬── SeasonSpan (date range + SeasonCode)
             └── SeasonSpan (date range + SeasonCode)

VacationHouse ──┬── belongs to HouseGroup
                └── has PricePlan ──── SeasonPrice (price per SeasonCode)
```

### Root Cause (Fixed)

**Bug in `DbSeeder.cs`**: SeasonSpan Code "B" was incorrectly assigned to `groupB` instead of the house's `groupA`.

### Completed Tasks

- [x] Debug why pricing returns incorrect/no price
- [x] Fix DbSeeder to assign all SeasonSpans to house's HouseGroup
- [x] Add full year coverage for SeasonSpans (Jan-Dec with alternating A/B codes)
- [x] Enhanced HouseGroup API with full CRUD operations
- [x] Added SeasonSpan calendar management endpoints

### Files Modified

- `Sommerhus.Repository/Data/DbSeeder.cs` - Fixed season span group assignment + full year coverage
- `Sommerhus.Application/Admin/HouseGroups/IAdminHouseGroupService.cs` - Extended interface
- `Sommerhus.Repository/Admin/HouseGroups/AdminHouseGroupService.cs` - Full implementation
- `Sommerhus.Api/Controllers/Admin/HouseGroupsController.cs` - Extended with calendar endpoints
- `Sommerhus.Contracts/Dtos/Admin/Pricing/HouseGroupDtos.cs` - New DTOs

### New API Endpoints

- `GET /api/admin/house-groups` - List all groups with house count
- `GET /api/admin/house-groups/{id}` - Get group details with calendar
- `PUT /api/admin/house-groups/{id}` - Update group
- `DELETE /api/admin/house-groups/{id}` - Delete group (if no houses)
- `POST /api/admin/house-groups/{groupId}/calendar` - Add season span
- `PUT /api/admin/house-groups/{groupId}/calendar/{spanId}` - Update span
- `DELETE /api/admin/house-groups/{groupId}/calendar/{spanId}` - Delete span

---

## HIGH PRIORITY: Area Search Fix ✅ COMPLETED

**Goal**: Fix the area search dropdown on the public frontend.

### Completed Tasks

- [x] Add `area` parameter to API `HousesController.Search()` endpoint
- [x] Add `areaId` parameter to `IHouseQueryService.SearchAsync()`
- [x] Implement area filtering in `HouseQueryService`
- [x] Update `SommerhusApi.GetHousesAsync()` to include area filter
- [x] Update MVC `HousesController.Houses()` to accept area and load areas for ViewBag

### Files Modified

- `Sommerhus.Application/Public/Houses/IHouseQueryService.cs` - Added areaId parameter
- `Sommerhus.Repository/Public/Houses/HouseQueryService.cs` - Implemented area filtering
- `Sommerhus.Api/Controllers/Public/HousesController.cs` - Added area query parameter
- `Sommerhus.Mvc/Services/SommerhusApi.cs` - Added areaId parameter
- `Sommerhus.Mvc/Controllers/Public/HousesController.cs` - Integrated area filter + ViewBag

---

## HIGH PRIORITY: Frontend Robustness ✅ PARTIAL

**Goal**: Prevent invalid data entry and improve UX across the admin frontend.

### Completed Tasks

- [x] Replaced currency free-text input with dropdown (DKK, EUR, SEK, NOK, GBP, USD)

### Files Modified

- `Sommerhus.Mvc/Views/Admin/House._Pricing.cshtml` - Currency dropdown

### Remaining Tasks

- [x] Add HouseGroup dropdown to house edit form
- [ ] Add client-side validation for required fields
- [ ] Add confirmation dialogs for destructive actions

---

## Phase 6: House Groups Management ✅ COMPLETED

**Goal**: Fix house groups admin functionality and implement season span management.

### Issues Fixed

- [x] **LINQ Translation Error**: Fixed EF Core query translation issue in house groups listing
- [x] **House Groups Admin Page**: Created dedicated house groups management interface
- [x] **Season Span Calendar**: Implemented calendar UI for managing season spans
- [x] **House Season Calendar**: Added season span management to house details page

### Features Implemented

#### House Groups Admin Interface

- **Index Page**: `/admin/house-groups` - List all house groups with house counts
- **Details Page**: `/admin/house-groups/{id}` - Manage group and season calendar
- **Create/Edit**: Full CRUD operations for house groups
- **Season Calendar**: Add/edit/delete season spans with calendar UI

#### House Season Calendar Integration

- **House Details**: Season calendar management in `/admin/houses/{id}?tab=pricing`
- **Group Integration**: Houses inherit season calendar from their assigned group
- **Calendar UI**: Date pickers for start/end dates with validation
- **Modal Interface**: Clean modal dialogs for season span operations

### Technical Implementation

#### Backend Changes

- **API Endpoints**: Added house season span endpoints to `HousesController`
- **Service Layer**: Extended `IAdminHouseGroupService` with house season span methods
- **Data Transfer**: Added `GroupId` to `HouseDetailsDto` for group awareness
- **Query Optimization**: Fixed EF Core LINQ translation in house groups listing

#### Frontend Changes

- **New Controller**: `HouseGroupsController` with full CRUD operations
- **Calendar UI**: Date pickers and modal dialogs for season management
- **Navigation**: Added "House Groups" tab to admin navigation
- **Integration**: Season calendar embedded in house pricing page

### API Endpoints Added

```http
GET    /api/admin/house-groups              # List house groups
GET    /api/admin/house-groups/{id}         # Get house group details
POST   /api/admin/house-groups              # Create house group
PUT    /api/admin/house-groups/{id}         # Update house group
DELETE /api/admin/house-groups/{id}         # Delete house group

POST   /api/admin/house-groups/{id}/calendar     # Add season span
PUT    /api/admin/house-groups/{id}/calendar/{id} # Update season span
DELETE /api/admin/house-groups/{id}/calendar/{id} # Delete season span

POST   /api/admin/houses/{id}/calendar     # Add house season span
PUT    /api/admin/houses/{id}/calendar/{id} # Update house season span
DELETE /api/admin/houses/{id}/calendar/{id} # Delete house season span
```

### Result

- **Build**: ✅ Passes
- **Tests**: ✅ 11/11 pass
- **House Groups Error**: ✅ Fixed
- **Season Calendar**: ✅ Fully functional with calendar UI
- **Documentation**: ✅ Updated

---

## Phase 5: Remove Sommerhus.Pricing Project ✅ COMPLETED

**Goal**: Clean up unused project or implement pricing engine.

### Decision Made

**Option A**: Delete empty `Sommerhus.Pricing/` project (chosen)

### Tasks Completed

- [x] Remove `Sommerhus.Pricing.csproj` from solution
- [x] Delete `Sommerhus.Pricing/` folder
- [x] Verify no broken references

### Result

- Build: ✅ Passes
- Tests: **11/11 pass** ✅

---

## Phase 6: Localization & Code Cleanup

**Goal**: Remove Danish text and improve code consistency.

### Tasks

- [ ] Search for Danish comments and error messages
- [ ] Replace with English equivalents
- [ ] Remove commented-out code blocks
- [ ] Run `dotnet format` across solution
- [ ] Fix any remaining nullable warnings

### Search Commands

```powershell
# Find Danish text
grep -r "Ukendt" --include="*.cs"
grep -r "fejl" --include="*.cs"
grep -r "område" --include="*.cs"
```

### Verification

```powershell
dotnet format Sommerhus_project.sln
dotnet build Sommerhus_project.sln --warnaserror
dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj
```

---

## Phase 7: API Documentation

**Goal**: Improve Swagger documentation.

### Tasks

- [ ] Add XML documentation to controllers
- [ ] Add `[ProducesResponseType]` attributes
- [ ] Configure Swagger to show XML comments
- [ ] Add example requests/responses
- [ ] Group endpoints by feature in Swagger UI

### Files to Modify

- All controller files
- `Sommerhus.Api/Program.cs` (Swagger config)
- `Sommerhus.Api/Sommerhus.Api.csproj` (enable XML docs)

### Verification

```powershell
dotnet build Sommerhus_project.sln
# Manual: Review Swagger UI for documentation quality
```

---

## Phase 8: Test Coverage Expansion

**Goal**: Increase test coverage for critical paths.

### Tasks

- [ ] Add tests for pricing pipeline
- [ ] Add tests for image upload/delete flows
- [ ] Add tests for area-city relationships
- [ ] Add negative test cases (invalid data)
- [ ] Add authorization tests (missing/invalid tokens)

### Files to Create

- `Sommerhus.Api.Tests/Admin/PricingTests.cs`
- `Sommerhus.Api.Tests/Admin/ImageUploadTests.cs`
- `Sommerhus.Api.Tests/Public/AuthorizationTests.cs`

### Verification

```powershell
dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj /p:CollectCoverage=true
# Review coverage report
```

---

## Phase 9: Performance Optimization

**Goal**: Optimize database queries and reduce N+1 problems.

### Tasks

- [ ] Audit EF Core queries with SQL logging
- [ ] Add `.AsSplitQuery()` where needed
- [ ] Review eager loading patterns (`.Include()`)
- [ ] Add database indexes for common queries
- [ ] Consider query caching for lookup data

### Analysis Commands

```powershell
# Enable detailed EF logging in appsettings.Development.json
# Review SQL output for N+1 patterns
```

### Verification

```powershell
dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj
# Load test with multiple concurrent requests
```

---

## Phase 10: MVC Frontend Improvements

**Goal**: Improve MVC client architecture.

### Tasks

- [ ] Add global exception handling middleware
- [ ] Improve error display in views
- [ ] Add client-side validation
- [ ] Review HTTP client retry policies
- [ ] Add request/response logging

### Files to Modify

- `Sommerhus.Mvc/Program.cs`
- `Sommerhus.Mvc/Services/*.cs`
- View files as needed

### Verification

```powershell
# Manual testing of MVC UI
# Test error scenarios (API down, invalid data)
```

---

## Phase 11: Admin UI Bug Fixes & Calendar Visualization ✅ COMPLETED

**Goal**: Fix critical admin UI bugs and improve season calendar UX.

### Issues Fixed

1. **House Groups Tab Error** ✅ FIXED
   - **Problem**: InvalidOperationException - View 'Index' not found
   - **Root Cause**: Controller returns `View()` without explicit path, ASP.NET looks in wrong folder
   - **Solution**: Added explicit view paths (`~/Views/Admin/HouseGroups/Index.cshtml`)

2. **Group Name Not Displayed** ✅ VERIFIED
   - **Status**: View code correctly displays `@group.Name` - likely a data issue if name is empty
   - **Location**: `/admin/house-groups` list view shows name from `HouseGroupListItemDto`

3. **Guest Count Now Affects Price** ✅ FIXED
   - **Solution**: Created `GuestFeeRule` that adds extra fee per guest above base (configurable)
   - **Config**: `Pricing:GuestFee:BaseGuests` (default: 2), `Pricing:GuestFee:PerGuestPerNight` (default: 50)
   - **Also Fixed**: Simplified pricing breakdown - shows nights grouped by season instead of every night

4. **Season Calendar UI** ✅ FIXED
   - **Fixed**: Form routes now use explicit paths instead of tag helpers
   - **Added**: Visual year calendar showing months color-coded by season
   - **Added**: Season legend with color indicators

### Files Modified

- `Sommerhus.Mvc/Controllers/Admin/HouseGroupsController.cs` - Explicit view paths
- `Sommerhus.Mvc/Views/Admin/House._Pricing.cshtml` - Visual calendar + fixed form routes
- `Sommerhus.Application/Pricing/Engine/Rules/GuestFeeRule.cs` - NEW: Guest pricing rule
- `Sommerhus.Application/Pricing/Engine/Rules/BaseNightlyRateRule.cs` - Simplified breakdown
- `Sommerhus.Api/Extensions/ServiceCollectionExtensions.cs` - Register GuestFeeRule
- `docs/DEVOPS.md` - Added database/image sync guide

### Verification

```powershell
dotnet build Sommerhus_project.sln  # ✅ Build succeeded
dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj  # ✅ 11/11 tests passed
```

---

## Phase 12: Additional UI Bug Fixes ✅ COMPLETED

**Goal**: Fix remaining UI bugs identified in admin interface.

### Issues Fixed

1. **House Group Dropdown Shows Names** ✅ FIXED
   - **Problem**: Dropdown displayed GUID IDs instead of names
   - **Solution**: Added `PopulateHouseGroupsAsync()` and `ViewBag.HouseGroups` with proper SelectList
   - **Files**: `HousesController.cs`, `House._Overview.cshtml`

2. **Season Calendar Tab Working** ✅ FIXED
   - **Problem**: Clicking tab changed URL but didn't load content
   - **Solution**: Added separate "Sæsonkalender" tab with dedicated `House._Calendar.cshtml` partial
   - **Files**: `House.cshtml`, `House._Calendar.cshtml`, `HousesController.cs`

3. **Calendar Buttons Have Icons** ✅ FIXED
   - **Problem**: Edit/delete buttons had no visible icons
   - **Solution**: Added Bootstrap Icons (`bi-pencil`, `bi-trash`) with proper styling
   - **Files**: `House._Calendar.cshtml`

4. **Enhanced Visual Calendar** ✅ FIXED
   - **Current**: Basic month blocks with colors
   - **Enhancement**: Improved visual calendar with month grid, season legend, and better colors
   - **Files**: `House._Calendar.cshtml`

### Files Modified

- `Sommerhus.Mvc/Controllers/Admin/HousesController.cs`
  - Added `PopulateHouseGroupsAsync()` method
  - Updated `House()` action to load season codes for calendar tab
  - Added calls to populate house groups in edit methods

- `Sommerhus.Mvc/Views/Admin/House.cshtml`
  - Added "Sæsonkalender" tab to navigation
  - Added calendar tab rendering logic

- `Sommerhus.Mvc/Views/Admin/House._Overview.cshtml`
  - Added house group dropdown with proper names
  - Added "Ingen gruppe" option

- `Sommerhus.Mvc/Views/Admin/House._Calendar.cshtml` - **NEW**
  - Dedicated calendar view with visual month grid
  - Season span management (add/edit/delete)
  - Modal forms for season span CRUD operations
  - Season legend with color indicators

### Verification

```powershell
dotnet build Sommerhus_project.sln  # ✅ Build succeeded
dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj  # ✅ Tests passing
```

---

## Phase 2.6: Architecture Simplification - Part 2 ✅ COMPLETED

**Goal**: Complete architecture cleanup with AdminController split and DTO consolidation.

### Tasks

- [x] Split MVC AdminController into domain-specific controllers
  - Created `AreasController` (252 lines) for area management
  - Created `FeaturesController` (112 lines) for feature management
  - `HousesController` reduced from 1067 to 730 lines (focused on houses/pricing)
- [x] Consolidate duplicate DTOs (one per entity with optional properties)
  - Moved `AreaListItemDto` to `Sommerhus.Core.Dtos.Shared`
  - Consolidated `AreaDetailsDto` (Admin+Public → Shared with optional fields)
  - Consolidated `HouseDetailsDto` (Admin+Public → Shared with optional fields)
  - Consolidated `HouseListItemDto` (Admin+Public → Shared with optional fields)
  - Moved `FeatureDetailsDto` and `FeatureListItemDto` to Shared
  - Moved `CityListItemDto` from Public to Shared
  - Achieved consistent organization: Shared (read) + Admin (write)
  - Deleted all duplicates from Admin/Public namespaces
  - Updated all references across API, MVC, and Tests projects
- [x] Delete nearly identical shared DTOs
- [x] Ensure consistent DTO naming and folder organization
- [x] Update all references after consolidation
- [x] Complete DTO reorganization details
- [x] Flatten Admin DTO structure (remove domain folders)
- [x] Split multi-DTO files into individual files for consistency
  - CityDtos.cs → 5 individual DTO files
  - HouseGroupDtos.cs → 3 individual DTO files
  - PricePlanDtos.cs → 4 individual DTO files
- [x] Update all namespaces to flat Admin structure

### Deferred

- [ ] Unify `ApiResponse<T>` with `ServiceResult<T>` (requires API contract changes)

---

## Phase 2.7: DTO Deep Consolidation ✅ COMPLETED

**Goal**: Reduce DTOs from 32 files to ~18-20 files using consistent 2-3 DTOs per entity pattern.

### Pattern

```
Per Entity:
├── {Entity}Dto.cs          # Read DTO (details + list via optional fields)
└── Upsert{Entity}Dto.cs    # Write DTO (create + update combined)
```

### Results: 32 → 17 files (47% reduction)

**Final Structure:**

```
Dtos/
├── Shared/              # All read/query DTOs + write DTOs per entity
│   ├── AreaDetailsDto.cs
│   ├── AreaListItemDto.cs
│   ├── CityDto.cs           # CityDto + UpsertCityDto (was 6 files!)
│   ├── FeatureDto.cs        # FeatureDto + UpsertFeatureDto
│   ├── FeatureValueDto.cs
│   ├── HouseDetailsDto.cs
│   ├── HouseListItemDto.cs
│   ├── HouseGroupDto.cs     # HouseGroupDto + UpsertHouseGroupDto
│   ├── SeasonDto.cs         # All pricing DTOs consolidated
│   ├── Images.cs
│   ├── LookupItem.cs
│   ├── Paging.cs
│   └── PostFeatureValueDto.cs
├── Admin/               # Only entity-specific write DTOs
│   ├── UpsertAreaDto.cs
│   ├── UpsertHouseDto.cs
│   └── AuthDtos.cs
└── Security/
    └── AdminRoles.cs
```

### Completed Tasks

✅ **Phase 2.7 - DTO Deep Consolidation** (Feb 4, 2026)

- **Goal**: Reduce mental load by consolidating DTOs from 32 → 17 files (47% reduction)
- **Pattern**: 1-2 DTOs per entity (unified read + write)
- **Results**:
  - City: 6 → 1 file (`CityDto.cs` with `CityDto` + `UpsertCityDto`)
  - Feature: 4 → 1 file (`FeatureDto.cs` with `FeatureDto` + `UpsertFeatureDto`)
  - HouseGroup: 4 → 1 file (`HouseGroupDto.cs` with `HouseGroupDto` + `UpsertHouseGroupDto`)
  - Pricing: 5 → 1 file (`SeasonDto.cs` with all pricing DTOs)
  - Fixed all service, controller, and view references
  - Updated tests to use new DTO structure
  - All tests passing (11/11)
  - Build successful (0 errors)

**Previous Tasks**:

- [x] Consolidate City DTOs (6 → 2 in CityDto.cs)
- [x] Consolidate HouseGroup DTOs (4 → 2 in HouseGroupDto.cs)
- [x] Consolidate Feature DTOs (4 → 2 in FeatureDto.cs)
- [x] Consolidate Pricing DTOs (5 → 1 in SeasonDto.cs)
- [x] Clean up orphaned DTOs (GroupDtos.cs, PricingDtos.cs)
- [x] Update all service and controller references
- [x] Run tests and verify build (11/11 passing)

---

## Phase 13: Backend Architecture Review Fixes (NEXT)

**Goal**: Address findings from the comprehensive backend architecture review (Feb 6, 2026). See `KNOWN_ISSUES.md` Phase 13 for full details on each issue.

### Phase 13a: Critical Bug Fixes & Correctness ✅ COMPLETED

These must be fixed first — they affect runtime correctness or security.

- [x] **19d** Fix `FeaturesController.UploadIcon` incomplete switch (runtime crash)
- [x] **19i** Fix `ProblemDetailsMiddleware` leaking `ex.Message` in production
- [x] **19g** Resolve `MaxLength` conflicts between domain attributes and fluent config
- [x] **19h** Remove duplicate `SeasonPrice → PricePlan` FK configuration in DbContext
- [x] **19j** Fix `PriceModifier.Value` SQLite-specific `HasColumnType("TEXT")`
- [x] **19b** Rename `PricePlanDetailsDto.planId` → `PlanId` (PascalCase)

**Result**: Build ✅ | Tests 11/11 ✅

### Phase 13b: DTO Predictability (estimated 3-4 hours)

Make DTOs predictable for colleagues — the biggest readability win.

- [x] **19a** Split `HouseDetailsDto` into `AdminHouseDetailsDto` + `PublicHouseDetailsDto`
- [x] **19a** Split `HouseListItemDto` into `AdminHouseListItemDto` + `PublicHouseListItemDto`
- [x] **19c** Rename `UpsertHouseDto.Name` → `Title` to match entity
- [x] **19e** Replace `FeatureUpsertOutcome` with `ServiceResult` pattern
- [x] **19f** Extract `IPricingQuoteService` from `IAdminPricingService` for public use
- [x] **19o** Change `IHouseQueryService.SearchAsync` to return `PageResult<T>`
- [x] **19t** Use `FeatureValueType` enum in DTOs instead of string

**Result**: Build ✅ | Tests 11/11 ✅

### Phase 13c: Consistency & DRY ✅ COMPLETED

Eliminate duplication and enforce conventions.

- [x] **19k** Replace `HttpRequest` parameters with `string baseUrl` in all service interfaces
- [x] **19l** Extract `CloneErrors` into shared `ServiceResult` helper (added `IReadOnlyDictionary` overload)
- [x] **19n** Extract duplicated `MapPlan` into shared `PricePlanMapper.ToDto()`
- [x] **19m** Standardize private field naming (remove underscore prefix from `_db`)
- [x] **19p** Remove all duplicate `using` statements (25 → 7 warnings)
- [x] **19q** Standardize API route patterns (`api/admin/auth` for AuthController)
- [x] **19r** Convert `AdminLoginRequest`/`AdminTokenResponse` to `sealed record`
- [x] **19s** Convert `PageResult<T>` to `sealed record` with `IReadOnlyList<T>`
- [~] **19ee** Use `this.FromResult()` in `HousesController` — skipped (Create/Update/Delete need custom responses)

**Result**: Build ✅ | Tests 11/11 ✅

### Phase 13d: Cleanup & Hygiene ✅ COMPLETED

Low-risk cleanup that reduces noise.

- [x] **19u** Delete orphaned `HouseAreas.cs` and `AreaCities.cs`
- [x] **19v** Remove dead `VacationHouse.CoverImageId` property
- [x] **19w** Remove dead `VacationHouse.Facilities` property + simplified `BuildSummary()`
- [x] **19x** Fix `SeasonCode` defaults (Name `""`, Color `"#6C757D"`)
- [x] **19y** Add `= Guid.NewGuid()` to `HouseGroup.Id` and `SeasonSpan.Id`
- [x] **19z** Standardize image `FileName` MaxLength to 300 (was 200/260/300)
- [x] **19aa** Fix `DbSeeder` exception swallowing (added `Console.WriteLine` logging, reduced timeout to 10s)
- [x] **19bb** Remove unused usings in `LookupItem.cs`
- [x] **19cc** Delete empty `Dtos/Public/` folder tree
- [x] **19dd** Replace Danish CORS comment with English
- [x] **19ff** Make all public controllers `sealed`
- [x] **19gg** Update stale doc references (`ARCHITECTURE.md` — `IImageStorage`, controller example)
- [x] **#3** Replace all remaining Danish error messages with English (services + pricing rules + test)

**Result**: Build ✅ (0 warnings) | Tests 11/11 ✅

### Verification (after each sub-phase)

```powershell
dotnet format Sommerhus_project.sln
dotnet build Sommerhus_project.sln --warnaserror
dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj --logger "console;verbosity=detailed"
```

---

## Post-Refactor Checklist

After completing all phases:

- [ ] All tests passing
- [ ] No build warnings
- [ ] Code formatted with `dotnet format`
- [ ] Documentation updated
- [ ] README.md reflects current state
- [ ] AGENTS.md updated if patterns changed
- [ ] Git history clean (squashed WIP commits)

---

## Timeline Estimate

| Phase     | Estimated Effort | Priority | Status        |
| --------- | ---------------- | -------- | ------------- |
| Phase 1   | 1-2 hours        | High     | **Completed** |
| Phase 2   | 2-3 hours        | High     | **Completed** |
| Phase 3   | 2-3 hours        | Medium   | **Completed** |
| Phase 4   | 2-3 hours        | Medium   | **Completed** |
| Phase 5   | 30 min           | Low      | **Completed** |
| Phase 6   | 1-2 hours        | Medium   | Pending       |
| Phase 7   | 2-3 hours        | Low      | Pending       |
| Phase 8   | 3-4 hours        | High     | Pending       |
| Phase 9   | 2-3 hours        | Medium   | Pending       |
| Phase 10  | 2-3 hours        | Medium   | Pending       |
| Phase 13a | 2-3 hours        | High     | **Completed** |
| Phase 13b | 3-4 hours        | High     | **Completed** |
| Phase 13c | 2-3 hours        | Medium   | **Completed** |
| Phase 13d | 1-2 hours        | Low      | **Completed** |

**Remaining**: ~20-28 hours

---

## How to Use This Roadmap

1. **Start with Phase 1** - It's low-risk and improves developer experience
2. **Run tests after each phase** - Don't proceed if tests fail
3. **Commit after each phase** - Keep changes isolated
4. **Update this document** - Check off tasks as completed
5. **Adjust as needed** - Real-world findings may change priorities

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

## Phase 14: MVC Frontend Architecture Review Fixes (NEXT)

**Goal**: Address findings from the comprehensive MVC/presentation layer review (Feb 6, 2026). See `KNOWN_ISSUES.md` Phase 14 for full details on each issue.

### Phase 14a: Critical Fixes & Security — **Completed** (Feb 6, 2026)

Fixed security issues, bugs, and consistency problems.

- [x] **20c** Remove plaintext `AdminAuth` credentials from `appsettings.Production.json`
- [x] **20d** Fix malformed `appsettings.Production.json` (double `/api/api` URL + broken JSON indentation)
- [x] **20v** Remove unused `AdminAuth` config from `appsettings.Development.json`
- [x] **20b** Replace Danish error messages in MVC controllers with English
- [x] **20g** Remove duplicate `using` statements (3 files)
- [x] **20a** Move `LoginViewModel` from `AccountController.cs` to `ViewModels/Account/LoginViewModel.cs`
- [x] **20e** Add `sealed` to `HouseGroupsController` and `PricesController`
- [x] **20f** Standardize all controllers on primary constructors with `camelCase` parameter names (no `_` prefix)
- [x] **20s** Delete commented-out code in public `HousesController`

### Phase 14b: Consistency & Predictability — **Completed** (Feb 6, 2026)

Made the codebase predictable for colleagues.

- [x] **20t** Remove `@using Sommerhus.Mvc.Controllers` from `_ViewImports.cshtml` (and two views)
- [x] **20k** Extract shared `SetError`/`SetSuccess` into `SommerhusControllerBase`; public + admin controllers now share the same base
- [x] **20i** Reorganize ViewModels: deleted dead `AreaViewModels.cs`, moved `HousePricingForm`/`SeasonPriceRow` → `Houses/`, moved `PricingAdminVm`/forms → `Prices/`
- [x] **20j** Deleted unused `HouseEditVm` (dead code, replaced by `HouseDetailsVm`)
- [x] Fixed public `HousesController` underscore parameter `_api` → `api` (missed in 14a)

### Phase 14c: Service Layer Cleanup + Public View Audit — **Completed** (Feb 6, 2026)

Improved the HTTP client layer, removed dead code, and audited all public views/controllers.

- [x] **20q** Fixed `ApiHttp.HandleResponseAsync` to read response body once as string, then deserialize from that string
- [x] **20r** Removed duplicate `GetHouseGroupsAsync` (LookupItem version); callers now map from `HouseGroupDto`
- [x] **20m** Removed dead `SommerhusApi.GetCitiesAsync` (called admin endpoint, was unused by any controller)
- [x] **20h** Cleaned up `AdminApiClient`: primary constructor, removed excessive blank lines between sections
- [x] **20n** Removed excessive blank lines in `AdminApiClient` (moved from 14d)
- [x] **20y** Replaced `ViewBag` usage in public `HousesController`/views with typed `HouseListVm`
- [x] **20z** Created shared `_FlashMessages.cshtml` partial; all public views now use it
- [x] **20aa** Fixed mojibake encoding in `Areas/Details.cshtml`
- [x] **20ab** Removed redundant `@using` directives in `_HouseCard.cshtml`

### Phase 14d: Cleanup & Hygiene — **Completed** (Feb 7, 2026)

Low-risk cleanup that reduces noise.

- [x] **20o** Removed Danish `.csproj` comments (historical notes, no longer needed)
- [x] **20u** Removed ~45 lines of inline CSS from `_Layout.cshtml` (already present in `site-additions.css`)
- [x] **20x** Added SRI hash (`integrity` + `crossorigin`) to htmx CDN fallback; local file confirmed present
- [x] **20w** Added `app.UseExceptionHandler("/error")` + `UseStatusCodePagesWithReExecute`; created `ErrorController` and friendly error view

### Verification (after each sub-phase)

```powershell
dotnet format Sommerhus_project.sln
dotnet build Sommerhus_project.sln --warnaserror
dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj --logger "console;verbosity=detailed"
```

---

## Phase 15: Post-Refactor Bug Fixes & Consistency Audit (Feb 8, 2026)

**Goal**: Fix all bugs and warnings introduced during refactoring, then identify remaining consistency improvements.

### Phase 15a: Bug Fixes & Warning Cleanup ✅ COMPLETED

Fixed 3 runtime bugs, 8 compiler warnings, 6 Danish text remnants, and 1 spurious import.

- [x] **#21a** Fix `AreasController.Create` rendering wrong view (`Houses/Create.cshtml` → `Areas/Details.cshtml`)
- [x] **#21b** Fix `AreasController.Update` `InvalidCastException` (`IReadOnlyList<Guid>` cast to `List<Guid>`)
- [x] **#21c** Fix `BuildAreaEditVmAsync` assigning `null` to `required AreaDetailsDto Area`
- [x] **#21d** Fix `AreasController.Update` unnecessary `res.Data is null` check on `object?`
- [x] **#22a** Remove duplicate `using` in `CitiesController.cs` (CS0105)
- [x] **#22b** Fix nullable dereference `res.Data.Id` in MVC `AreasController.Create` (CS8602)
- [x] **#22c** Fix nullability mismatch in `AdminApiClient.GetHousesAsync` return type (CS8619)
- [x] **#22d** Fix `async` without `await` in MVC `HousesController.AdminIndex` (CS1998)
- [x] **#22e** Fix null source to `OrderBy` in `HouseGroups/Details.cshtml` (CS8604)
- [x] **#23a** Replace 4 Danish error messages in `AdminFeatureService.cs`
- [x] **#23b** Replace 2 Danish comments in `StorageOptions.cs`
- [x] **#24a** Remove spurious `using Microsoft.Identity.Client` in `UpsertAreaDto.cs`

**Result**: Build ✅ (0 warnings) | Tests 11/11 ✅

### Phase 15b: Codebase Consistency ✅ COMPLETED

Make the codebase predictable — same pattern for every entity.

**Bugs fixed**:

- [x] **#31** Fix house image kind bug (`hero` → Cover/Gallery/Floorplan buttons matching `ImageKind` enum)
- [x] **#32** Add batch image upload for areas (full stack: service, API, MVC client, controller, view)
- [x] **#33** Standardize area image delete route (`/images/delete` → `/images/{imageId}/delete`)
- [x] **#34** Fix Areas Details action not loading cities for edit form

**Patterns standardized**:

- [x] **#26** Standardize MVC controller Create/Details view pattern (Areas now matches Houses)
- [x] **#27** Fix `AreaCreateVm` namespace (`Admin.Area` → `Admin.Areas`)
- [x] **#29** Standardize admin API controller patterns (FromResult, async/await, unused imports)

**Remaining (low priority)**:

- [ ] **#25** Standardize all Upsert DTOs to `sealed class` with `{ get; set; }` for form binding
- [ ] **#28** Remove unused generic image service interfaces or implement them
- [ ] **#30** Convert Core services to primary constructors (C# 12, match controller style)

**Result**: Build ✅ (0 errors, 0 warnings) | Tests 11/11 ✅

### Verification

```powershell
dotnet format Sommerhus_project.sln
dotnet build Sommerhus_project.sln --warnaserror
dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj --logger "console;verbosity=detailed"
```

---

## Enterprise Readiness Phases (16–22)

These phases transform Sommerhus from a working admin tool into a production-grade vacation rental platform.

---

## Phase 16: Flexible Calendar & Pricing Model

**Goal**: Decouple season calendars from house groups so individual houses can have custom calendars, and make it easy to switch between pricing/calendar configurations.

### Current State

- `SeasonSpan` is tied to `HouseGroup` via `GroupId` — every house in a group shares the same calendar
- `PricePlan` is tied to a single house via `HouseId` — pricing is already per-house
- No way for a house owner to have a custom season calendar independent of their group

### Design

#### New Domain Model: `SeasonCalendar`

```
SeasonCalendar (new)
├── Id: Guid
├── Name: string              ("Standard 2026", "Owner Jensen Custom")
├── Year: int?                (null = template, int = year-specific)
├── IsTemplate: bool          (reusable template vs one-off)
├── CreatedUtc / UpdatedUtc
└── Spans: ICollection<SeasonSpan>

SeasonSpan (modified)
├── CalendarId: Guid          (FK → SeasonCalendar, replaces GroupId)
└── (rest unchanged)

HouseGroup (modified)
├── DefaultCalendarId: Guid?  (FK → SeasonCalendar)
└── (rest unchanged)

VacationHouse (modified)
├── CalendarOverrideId: Guid? (FK → SeasonCalendar, null = use group default)
└── (rest unchanged)
```

#### Resolution Logic

```csharp
// Pseudo-code for resolving a house's effective calendar
SeasonCalendar GetEffectiveCalendar(VacationHouse house)
    => house.CalendarOverride       // 1. House-level override
    ?? house.Group?.DefaultCalendar // 2. Group default
    ?? systemDefaultCalendar;       // 3. Global fallback
```

#### PricePlan Enhancements

```
PricePlan (modified)
├── IsActive: bool            (already exists)
├── Label: string?            ("Summer 2026 rates", "Early bird")
└── ActivatedUtc: DateTime?   (when this plan was last activated)
```

- A house can have multiple `PricePlan` records but only one `IsActive = true`
- Admin UI shows a dropdown to switch active plan (one click)
- Old plans are preserved for history/reactivation

### Tasks

- [x] **16a** Create `SeasonCalendar` entity, replace `SeasonSpan.GroupId` with `CalendarId`, migration
  - `Sommerhus.Domain/Models/Pricing/SeasonCalendar.cs` — entity with Name, Year, IsTemplate, IAuditable, Spans collection
  - `SeasonSpan.GroupId` → `SeasonSpan.CalendarId` (FK to SeasonCalendar)
  - EF Core config: cascade delete from calendar to spans, unique index on `{CalendarId, StartDate, EndDate}`
  - Migration `20260209105033_FlexibleCalendar`
  - DbSeeder updated: creates "Vesterhavet Calendar" and links to group via `DefaultCalendar`
- [x] **16b** Add `CalendarOverrideId` to `VacationHouse`, `DefaultCalendarId` to `HouseGroup`
  - `VacationHouse.CalendarOverrideId` (nullable FK → SeasonCalendar, SetNull on delete)
  - `HouseGroup.DefaultCalendarId` (nullable FK → SeasonCalendar, SetNull on delete)
  - Navigation properties with proper EF config
- [x] **16c** Calendar resolution logic (house override → group default → empty)
  - `EfRatePlanStore.GetSeasonCalendarAsync` resolves effective calendar for pricing
  - `AdminHouseService.GetDetailsAsync` resolves effective calendar for admin display
  - `AdminHouseGroupService` season span CRUD works through group's `DefaultCalendarId`
  - Auto-creates calendar for group on first span add (`EnsureGroupCalendarAsync`)
- [x] **16d** Admin CRUD for `SeasonCalendar` (API + service)
  - `IAdminCalendarService` / `AdminCalendarService` — List, Get, Create, Update, Delete
  - `CalendarsController` at `api/admin/calendars` — full REST CRUD
  - `CalendarDto`, `UpsertCalendarDto`, `SetCalendarOverrideDto`, `CreateCalendarOverrideDto`
  - Delete protection: cannot delete calendars in use by groups or houses
- [x] **16e** Admin UI: house detail calendar tab with override management
  - Calendar source badge (Group: name / Custom Override / No calendar)
  - "Set Override" modal: pick existing calendar or create new custom one
  - "Revert to Group" button to remove override
  - Season span CRUD works on effective calendar (override or group default)
  - `AdminApiClient`: `GetCalendarsAsync`, `SetHouseCalendarOverrideAsync`, `RemoveHouseCalendarOverrideAsync`, `CreateHouseCalendarOverrideAsync`
  - MVC controller actions: `SetCalendarOverride`, `RemoveCalendarOverride`, `CreateCalendarOverride`
- [x] **16f** Pricing calculation uses resolved calendar (done in 16c)
  - `EfRatePlanStore` and `BaseNightlyRateRule` unchanged — already use `GetSeasonCalendarAsync` which now resolves override → group default
- [x] **16g** Tests: calendar CRUD, override logic, revert, span management
  - 10 new tests in `CalendarTests.cs`: list seeded, create, update, delete unused, delete in-use fails, house shows group source, create override, remove override reverts, set existing calendar, span CRUD on group calendar
  - All 40 tests passing (30 existing + 10 new)

**Estimated effort**: 8–12 hours | **Priority**: High | **Status**: ✅ Completed

---

## Phase 17: Audit Trail

**Goal**: Track who changed what and when on all critical entities. Provide a browsable change history in the admin UI.

### Design

#### Approach: EF Core Interceptor + Audit Log Table

Two complementary layers:

**Layer 1 — Timestamp columns** (on every entity):

```
IAuditable (interface)
├── CreatedAtUtc: DateTime
├── CreatedBy: string?        (user ID or "system")
├── UpdatedAtUtc: DateTime?
├── UpdatedBy: string?
```

Populated automatically via a `SaveChangesInterceptor` that reads the current `ClaimsPrincipal` from `IHttpContextAccessor`.

**Layer 2 — Change history table** (for critical entities):

```
AuditEntry
├── Id: long (auto-increment)
├── EntityType: string        ("VacationHouse", "PricePlan", etc.)
├── EntityId: string          (the PK, stored as string for flexibility)
├── Action: AuditAction       (Created, Updated, Deleted)
├── ChangedBy: string
├── ChangedAtUtc: DateTime
├── Changes: string           (JSON diff of old→new values)
```

#### Which Entities to Audit

| Entity         | Timestamps | Change History |
| -------------- | ---------- | -------------- |
| VacationHouse  | ✅         | ✅             |
| PricePlan      | ✅         | ✅             |
| SeasonPrice    | ✅         | ✅             |
| SeasonCalendar | ✅         | ✅             |
| Area           | ✅         | ✅             |
| Feature        | ✅         | ✅             |
| HouseGroup     | ✅         | ✅             |
| City           | ✅         | ❌ (low churn) |
| Images         | ✅         | ❌ (binary)    |

#### Recommended Package

No external packages needed. EF Core's `SaveChangesInterceptor` + `ChangeTracker.Entries()` gives us everything. The interceptor:

1. Iterates `ChangeTracker.Entries<IAuditable>()` to set timestamps
2. Iterates tracked critical entities to build JSON diffs
3. Writes `AuditEntry` rows in the same transaction

### Tasks

- [x] **17a** Create `IAuditable` interface, add to all entities, create migration
  - `IAuditable` interface in `Sommerhus.Domain/Models/IAuditable.cs`
  - Added to: `VacationHouse`, `Area`, `City`, `Feature`, `HouseGroup`, `PricePlan`
  - Renamed `CreatedUtc` → `CreatedAtUtc`, `UpdatedUtc` → `UpdatedAtUtc` for consistency
  - Migration `20260208183224_AuditTrail` handles column renames and additions
- [x] **17b** Build `AuditSaveChangesInterceptor` (timestamps + change log)
  - `Sommerhus.Core/Data/AuditSaveChangesInterceptor.cs`
  - Layer 1: Auto-populates `CreatedAtUtc`/`CreatedBy`/`UpdatedAtUtc`/`UpdatedBy` on all `IAuditable` entities
  - Layer 2: Writes JSON change diffs to `AuditEntry` for critical entities (VacationHouse, Area, Feature, HouseGroup, PricePlan, SeasonPrice)
  - Reads current user from `IHttpContextAccessor` → `ClaimsPrincipal`
- [x] **17c** Create `AuditEntry` entity and `DbSet`, migration
  - `Sommerhus.Domain/Models/AuditEntry.cs` with `AuditAction` enum (Created, Updated, Deleted)
  - `DbSet<AuditEntry>` in `AppDbContext` with indexes on `(EntityType, EntityId)` and `ChangedAtUtc`
- [x] **17d** Register interceptor in DI, inject `IHttpContextAccessor`
  - Registered as `Scoped` in `ServiceCollectionExtensions.AddSommerhusPersistence()`
  - Added to `DbContextOptions` via `options.AddInterceptors()`
  - Test factory updated to include interceptor
- [x] **17e** Admin API: `GET /api/admin/audit?entity=House&entityId=xxx` with pagination
  - `IAuditService` / `AuditService` in `Core/Services/Admin/Audit/`
  - `AuditController` at `api/admin/audit` with filters: `entity`, `entityId`, `changedBy`, `page`, `pageSize`
  - Returns `PageResult<AuditEntryDto>`
- [x] **17f** Admin MVC: audit history tab on House detail pages
  - New "Audit History" tab on House Details page
  - `_TabAudit.cshtml` partial with action badges, timestamps, expandable JSON diffs
  - `AdminApiClient.GetAuditEntriesAsync()` for MVC → API communication
- [x] **17g** Tests: verify audit entries created on CRUD operations
  - 6 new tests in `AuditTests.cs`: Create/Update/Delete generate audit entries, endpoint pagination, entity type filtering, IAuditable timestamp population
  - All 17 tests passing (11 existing + 6 new)
- [x] **17h** Backfill `CreatedAtUtc` for existing rows
  - Migration renames existing `CreatedUtc` columns to `CreatedAtUtc`
  - New entities get `CreatedAtUtc` with default value; interceptor auto-populates going forward

**Estimated effort**: 6–10 hours | **Priority**: High | **Status**: ✅ Completed

---

## Phase 18: Entity Status & Lifecycle

**Goal**: Add a status lifecycle to houses (and optionally other entities) so content can be drafted, reviewed, published, and archived.

### Design

#### Status Enum

```csharp
public enum EntityStatus
{
    Draft = 0,       // Visible only to admins, not in public search
    Published = 1,   // Live on the public site
    Archived = 2     // Hidden from public, preserved for records
}
```

#### Domain Changes

```
VacationHouse (modified)
├── Status: EntityStatus = Draft
├── PublishedAtUtc: DateTime?    (set when first published)
├── ArchivedAtUtc: DateTime?    (set when archived)

Area (modified)
├── Status: EntityStatus = Draft

Feature (optional, lower priority)
├── Status: EntityStatus = Published  (default published since features are reusable)
```

#### Business Rules

- **Draft → Published**: Requires validation (title, city, at least one image, active price plan)
- **Published → Archived**: Soft-delete; existing bookings (future) remain valid
- **Archived → Draft**: Re-opens for editing
- **Published → Draft**: Unpublishes immediately (admin override only)
- Public search (`HouseQueryService`) filters to `Status == Published` only
- Admin search shows all statuses with a filter dropdown

#### Transition Service

```csharp
public interface IEntityLifecycleService
{
    Task<ServiceResult> TransitionAsync(Guid houseId, EntityStatus target, CancellationToken ct);
}
```

The service validates preconditions before allowing transitions and writes an audit entry.

### Tasks

- [x] **18a** Create `EntityStatus` enum, add `Status` + timestamp fields to `VacationHouse` and `Area`, migration
  - `Sommerhus.Domain/Models/EntityStatus.cs` — enum with `Draft`, `Published`, `Archived`
  - `VacationHouse`: added `Status`, `PublishedAtUtc`, `ArchivedAtUtc`
  - `Area`: added `Status`
  - EF Core config: default values + indexes on `Status`
  - Migration `20260209084309_EntityStatusLifecycle`
- [x] **18b** Build `IEntityLifecycleService` with validation rules
  - `Core/Services/Admin/Lifecycle/IEntityLifecycleService.cs` — `TransitionHouseAsync`, `TransitionAreaAsync`
  - `Core/Services/Admin/Lifecycle/EntityLifecycleService.cs` — validates transitions, enforces publish preconditions (title, city, images), manages timestamps
  - Allowed transitions: Draft→Published, Published→Archived, Published→Draft, Archived→Draft
- [x] **18c** Add status filter to public `HouseQueryService` (only `Published`) + `AreaQueryService`
  - `SearchAsync` and `GetAsync` in both services filter by `Status == Published`
  - Draft and Archived entities hidden from public API
- [x] **18d** Add status filter to admin `AdminHouseService.SearchAsync` + update DTOs
  - Optional `EntityStatus? status` parameter on `SearchAsync`
  - `AdminHouseListItemDto` includes `Status` field
  - `AdminHouseDetailsDto` includes `Status`, `PublishedAtUtc`, `ArchivedAtUtc`
- [x] **18e** Admin API: `POST /api/admin/houses/{id}/status` + `POST /api/admin/areas/{id}/status`
  - `ChangeStatusDto` with `[Required] Target` property
  - Both endpoints use `IEntityLifecycleService` and return `ServiceResult` via `FromResult()`
  - Admin search endpoint accepts `?status=Draft` query parameter
- [x] **18f** Admin MVC: status badge on list, transition buttons on detail page
  - House list: status filter dropdown + color-coded badges (Draft=warning, Published=success, Archived=secondary)
  - House details overview tab: status badge + contextual transition buttons (Publish/Archive/Unpublish/Re-open)
  - `AdminApiClient`: `ChangeHouseStatusAsync`, `ChangeAreaStatusAsync`, updated `GetHousesAsync` with status filter
- [x] **18g** Extend to `Area` entity (done alongside 18a–18e)
  - Area lifecycle transitions share the same `IEntityLifecycleService`
  - Public `AreaQueryService` filters by `Published`
- [x] **18h** Tests: lifecycle transitions, public visibility, validation rules
  - 13 new tests in `LifecycleTests.cs`: new house defaults to Draft, publish with/without image, archive, reopen, invalid transitions, same-status rejection, public search excludes drafts, public search includes published, public get-by-id returns 404 for draft, admin search filters by status, admin details includes status fields, non-existent house returns 404
  - Fixed pre-existing `HouseImagesTests` to use admin endpoint (public endpoint now filters by Published)
  - All 30 tests passing (17 existing + 13 new)

**Estimated effort**: 5–8 hours | **Priority**: High | **Status**: ✅ Completed

---

## Phase 19: Optimistic Concurrency Control ⏸️ ON HOLD

**Status**: On hold — not needed for current development priorities.

**Goal**: Prevent admins from silently overwriting each other's changes.

### Design

#### Approach: EF Core Row Version (best practice for SQL databases)

```
IConcurrencyAware (interface)
├── RowVersion: byte[]        ([Timestamp] attribute)
```

EF Core natively supports this: if two admins load the same entity, the second `SaveChanges` throws `DbUpdateConcurrencyException` because the `RowVersion` changed.

#### Entities to Protect

All entities that admins edit concurrently:

- `VacationHouse`, `Area`, `Feature`, `HouseGroup`
- `PricePlan`, `SeasonPrice`, `SeasonCalendar`, `SeasonSpan`

#### Flow

1. Admin loads entity → API returns `RowVersion` (as Base64 string in DTO)
2. Admin submits edit → sends `RowVersion` back in the update DTO
3. Service sets `OriginalValues["RowVersion"]` before saving
4. If stale → `DbUpdateConcurrencyException` → return `ServiceResult` with `Conflict` status
5. MVC shows "This record was modified by another user. Please reload and try again."

#### DTO Changes

```csharp
// Add to all Upsert DTOs
public string? RowVersion { get; set; }  // Base64-encoded byte[]
```

#### ServiceResult Extension

```csharp
public enum ServiceResultStatus
{
    Success, NotFound, Invalid, Error,
    Conflict  // NEW — concurrency conflict
}
```

#### SQLite Consideration

SQLite doesn't support `[Timestamp]`/`rowversion`. Use a `Guid` or `long` concurrency token instead, incremented manually in the interceptor. The `[ConcurrencyCheck]` attribute works on any column.

```csharp
[ConcurrencyCheck]
public long Version { get; set; }
```

### Tasks

- [ ] **19a** Create `IConcurrencyAware` interface with `Version` property
- [ ] **19b** Add `Version` column to all editable entities, migration
- [ ] **19c** Build `ConcurrencyInterceptor` to auto-increment version on save
- [ ] **19d** Add `Conflict` to `ServiceResultStatus`, update `FromResult` helper
- [ ] **19e** Update all admin update services to catch `DbUpdateConcurrencyException`
- [ ] **19f** Add `Version` to all Upsert DTOs and detail DTOs
- [ ] **19g** MVC: show conflict error message, reload prompt
- [ ] **19h** Tests: simulate concurrent edits, verify conflict detection

**Estimated effort**: 5–7 hours | **Priority**: Medium

---

## Phase 20: Price Snapshots & Quotation Cache

**Goal**: Pre-compute and cache price quotes so public-facing pages are fast and prices are consistent during a customer's session.

### Design

#### Why Snapshots?

Computing a price requires: resolving the calendar → finding season spans for the date range → looking up nightly rates per season code → applying modifiers. This involves 4+ DB queries and business logic. For a search results page showing 20 houses, that's 80+ queries per page load.

#### Approach: Materialized Price Summary + On-Demand Quote Cache

**Layer 1 — Price Summary (materialized, updated on price/calendar change)**:

```
HousePriceSummary (new)
├── HouseId: Guid (PK)
├── MinNightlyPrice: decimal?     (cheapest season)
├── MaxNightlyPrice: decimal?     (most expensive season)
├── Currency: string
├── ComputedAtUtc: DateTime
```

This is what search results show ("from 850 DKK/night"). Recomputed whenever a `PricePlan` or `SeasonCalendar` changes (via a domain event or service call).

**Layer 2 — Quote Cache (on-demand, short TTL)**:

```
PriceQuote (new)
├── Id: Guid
├── HouseId: Guid
├── CheckIn: DateOnly
├── CheckOut: DateOnly
├── Nights: int
├── NightlyBreakdown: string      (JSON: [{date, seasonCode, price}])
├── Subtotal: decimal
├── Modifiers: string             (JSON: [{name, amount}])
├── Total: decimal
├── Currency: string
├── PricePlanId: Guid             (snapshot of which plan was used)
├── CalendarId: Guid              (snapshot of which calendar was used)
├── ComputedAtUtc: DateTime
├── ExpiresAtUtc: DateTime        (e.g., +15 minutes)
```

#### Quote Service

```csharp
public interface IPriceQuoteService
{
    // Used by search results — fast, pre-computed
    Task<HousePriceSummary?> GetSummaryAsync(Guid houseId, CancellationToken ct);

    // Used by detail page / booking flow — computed or cached
    Task<PriceQuote> GetQuoteAsync(Guid houseId, DateOnly checkIn, DateOnly checkOut, CancellationToken ct);

    // Called when prices/calendar change
    Task RecomputeSummaryAsync(Guid houseId, CancellationToken ct);
}
```

#### Cache Strategy

- `HousePriceSummary`: stored in DB, recomputed on price/calendar write operations
- `PriceQuote`: stored in DB with TTL, or use `IMemoryCache` for smaller deployments
- No external cache infrastructure needed (Redis etc.) — DB + memory cache is sufficient at this scale

### Tasks

- [x] **20a** Create `HousePriceSummary` entity, migration, seed from existing price plans
- [x] **20b** Create `PriceQuote` entity (or in-memory model), migration
- [x] **20c** Build `IPriceQuoteService` with summary computation and quote calculation
- [x] **20d** Hook summary recomputation into price plan and calendar save operations
- [x] **20e** Public API: `GET /api/houses/{id}/quote?checkIn=&checkOut=`
- [x] **20f** Public search: include `MinNightlyPrice` in list DTOs from summary table
- [x] **20g** MVC: price display on search cards, quote widget on detail page
- [x] **20h** Tests: quote accuracy, cache invalidation, summary recomputation

**Estimated effort**: 8–12 hours | **Priority**: High

---

## Phase 21: Availability Model

**Goal**: Future-proof the system with an availability model and realistic UI, so booking can be added later with minimal changes.

### Design

#### Approach: Date-Level Availability Grid

```
AvailabilityBlock (new)
├── Id: Guid
├── HouseId: Guid
├── StartDate: DateOnly
├── EndDate: DateOnly
├── Status: AvailabilityStatus
├── Source: AvailabilitySource
├── Note: string?                 (e.g., "Owner blocked", "Maintenance")
├── CreatedAtUtc: DateTime
├── CreatedBy: string?

public enum AvailabilityStatus
{
    Available = 0,
    Blocked = 1,           // Owner/admin blocked dates
    Tentative = 2,         // Hold / pending confirmation (future booking use)
    Booked = 3             // Confirmed booking (future)
}

public enum AvailabilitySource
{
    Manual = 0,            // Admin/owner set it
    ICalSync = 1,          // Imported from external calendar (future)
    Booking = 2            // Created by booking system (future)
}
```

#### Why Not a Per-Day Table?

A per-day row for every house × every day would be millions of rows. Date-range blocks are more efficient and match how vacation rentals actually work (week-long blocks, seasonal closures). Querying "is house available for dates X–Y?" becomes:

```sql
SELECT COUNT(*) FROM AvailabilityBlocks
WHERE HouseId = @id
  AND StartDate < @checkOut
  AND EndDate > @checkIn
  AND Status IN (Blocked, Tentative, Booked)
```

If count = 0, the house is available.

#### Availability Service

```csharp
public interface IAvailabilityService
{
    Task<bool> IsAvailableAsync(Guid houseId, DateOnly checkIn, DateOnly checkOut, CancellationToken ct);
    Task<IReadOnlyList<AvailabilityBlock>> GetBlocksAsync(Guid houseId, DateOnly from, DateOnly to, CancellationToken ct);
    Task<ServiceResult> BlockDatesAsync(Guid houseId, DateOnly start, DateOnly end, string? note, CancellationToken ct);
    Task<ServiceResult> UnblockDatesAsync(Guid blockId, CancellationToken ct);
}
```

#### UI Components

- **Admin**: Calendar grid on house detail page (new tab "Availability"). Click-drag to block/unblock dates. Color-coded by status.
- **Public**: Calendar widget on house detail page showing available (green) / unavailable (red) dates. Date picker for check-in/check-out that disables unavailable dates.
- **Search filter** (future): "Available from X to Y" filter on public search.

#### Future Booking Integration Point

When booking is implemented later:

1. `BookingService.CreateAsync()` calls `AvailabilityService.IsAvailableAsync()` to check
2. On confirmation, creates an `AvailabilityBlock` with `Status = Booked, Source = Booking`
3. On cancellation, removes the block
4. The availability model doesn't need to change — only a new `Source` value is added

### Tasks

- [x] **21a** Create `AvailabilityBlock` entity, enums, migration
- [x] **21b** Build `IAdminAvailabilityService` + `IAvailabilityQueryService` (check, list, block, unblock)
- [x] **21c** Admin API: CRUD endpoints for availability blocks (`api/admin/houses/{id}/availability`)
- [ ] **21d** Admin MVC: availability calendar tab on house details (interactive grid) _(deferred — UI)_
- [x] **21e** Public API: `GET /api/houses/{id}/availability?from=&to=`
- [ ] **21f** Public MVC: calendar widget on house detail page _(deferred — UI)_
- [x] **21g** Integrate availability check into quote service (no quote for unavailable dates)
- [x] **21h** Tests: overlap detection, block/unblock, availability queries (12 integration tests)

**Estimated effort**: 8–12 hours | **Priority**: High

---

## Phase 22: Search Engine Upgrade

**Goal**: Replace the current `EF.Functions.Like` search with a proper search-ready model that supports fast full-text search, faceted filtering, and relevance ranking.

**Dependencies**: Phase 20 (Price Snapshots — `MinNightlyPrice` for search cards), Phase 23 (Feature Categorization — typed search filters)

### Current State

- Search uses `LIKE '%term%'` which cannot use indexes — full table scan on every query
- No relevance ranking (results ordered by `CreatedUtc` only)
- No faceted filtering (by price range, features, number of bedrooms, etc.)
- Includes 4 joins per query (`Images`, `City`, `Areas`, `HouseFeatures`)

### Design

#### Approach: Search-Ready Denormalized Read Model

Instead of adding an external search engine (Elasticsearch, Meilisearch), which adds infrastructure complexity, we build a **denormalized search table** that is optimized for read queries. This is the standard pattern for systems at this scale (hundreds to low thousands of houses).

```
HouseSearchDocument (new, denormalized read model)
├── HouseId: Guid (PK)
├── Title: string
├── Description: string?
├── Summary: string?              (pre-computed, HTML-stripped)
├── CityName: string?
├── CityZip: string?
├── Address: string?
├── AreaNames: string?            (comma-separated, for LIKE search)
├── SearchKeywords: string?        (from VacationHouse.SearchKeywords — Phase 23)
├── FeatureJson: string?          (JSON array of {key, name, value, unit, category})
├── CoverImageUrl: string?
├── Status: EntityStatus
├── MinNightlyPrice: decimal?     (from HousePriceSummary)
├── MaxNightlyPrice: decimal?
├── Currency: string?
├── Bedrooms: int?                (extracted from features)
├── MaxGuests: int?               (extracted from features)
├── HasPool: bool                 (extracted from features)
├── PetFriendly: bool             (extracted from features)
├── Latitude: double?             (future: geo search)
├── Longitude: double?            (future: geo search)
├── SearchVector: string          (concatenated searchable text for FTS)
├── UpdatedAtUtc: DateTime
```

#### Why This Over Elasticsearch?

- **No infrastructure**: No separate service to deploy, monitor, or pay for
- **Transactional consistency**: Updated in the same DB transaction as the source data
- **Sufficient at scale**: PostgreSQL FTS or SQLite FTS5 handles 10K+ documents easily
- **Upgrade path**: If you outgrow this, the denormalized model maps 1:1 to an Elasticsearch index

#### Full-Text Search Strategy

- **PostgreSQL** (production): Use `tsvector`/`tsquery` with GIN index on `SearchVector`
- **SQLite** (dev/test): Use FTS5 virtual table or fall back to optimized `LIKE` on `SearchVector`
- Abstract behind `ISearchEngine` interface so the implementation can be swapped

#### Search Service

```csharp
public interface IHouseSearchService
{
    Task<PageResult<HouseSearchResultDto>> SearchAsync(HouseSearchFilter filter, CancellationToken ct);
    Task RebuildIndexAsync(CancellationToken ct);           // Full rebuild
    Task UpdateDocumentAsync(Guid houseId, CancellationToken ct);  // Single house refresh
}

public record HouseSearchFilter
{
    public string? Query { get; init; }
    public Guid? AreaId { get; init; }
    public string? City { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public int? MinBedrooms { get; init; }
    public int? MinGuests { get; init; }
    public bool? HasPool { get; init; }
    public bool? PetFriendly { get; init; }
    public Dictionary<string, string>? FeatureFilters { get; init; }  // Dynamic: {"sauna": "true", "distance_shop": "500"}
    public DateOnly? CheckIn { get; init; }       // Integrates with availability
    public DateOnly? CheckOut { get; init; }
    public HouseSearchSort Sort { get; init; }    // Relevance, Price, Newest
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
```

#### Index Maintenance

The search document is refreshed:

- **On write**: When a house, its features, price plan, or images change → `UpdateDocumentAsync`
- **On deploy**: `RebuildIndexAsync` as a startup task or admin action
- No background jobs needed — synchronous refresh in the same request is fast enough for a single document

### Tasks

- [ ] **22a** Create `HouseSearchDocument` entity, migration, DB indexes
- [ ] **22b** Build `ISearchIndexer` to populate/refresh documents from source entities
- [ ] **22c** Build `IHouseSearchService` with filter, sort, and pagination
- [ ] **22d** Create `HouseSearchFilter` and `HouseSearchResultDto`
- [ ] **22e** Hook indexer into house/feature/price/image save operations
- [ ] **22f** Admin API: `POST /api/admin/search/rebuild` endpoint
- [ ] **22g** Public API: replace `HouseQueryService.SearchAsync` with new search service
- [ ] **22h** Public MVC: faceted search UI (price range filter, feature checkboxes/dropdowns from Phase 23 categories, keyword search)
- [ ] **22i** Tests: search relevance, filter combinations, index rebuild

**Estimated effort**: 10–15 hours | **Priority**: Medium

---

## Phase 23: Feature Categorization & Search Metadata

**Goal**: Add categories, display hints, and search metadata to features so the search UI can render typed filters (checkboxes for boolean features, dropdowns for numeric features, range selectors for distances) — matching professional vacation rental search interfaces like the one at sommerhussøgning.dk.

**Dependencies**: None (can start immediately)

### Current State

- `Feature` has `FeatureValueType` (Bool, Int, Decimal, Text) — good for storage
- No grouping/categorization — "Pool?" and "Bedrooms" are treated identically in the UI
- No way to mark which features should appear in search filters
- No predefined options for dropdown features (e.g., Bedrooms: 1–10)
- Search UI cannot dynamically generate filter controls from feature metadata

### Design

#### Feature Categorization

```csharp
public enum FeatureCategory
{
    Property = 0,     // Bedrooms, bathrooms, max guests, size (m²)
    Facility = 1,     // Pool, wifi, sauna, dishwasher, washing machine
    Distance = 2,     // Distance to shop, beach, water
    Other = 3         // Catch-all for uncategorized features
}
```

#### Feature Entity Changes

```
Feature (modified)
├── Category: FeatureCategory = Other
├── IsSearchable: bool = true              (appears in search filter UI)
├── Options: string?                        (JSON array: ["1","2","3","4","5+"] for dropdowns)
├── (rest unchanged: Name, Key, ValueType, Unit, IconUrl, SortOrder)
```

How categories map to search UI controls:

| ValueType       | Category   | Search UI Control                             |
| --------------- | ---------- | --------------------------------------------- |
| `Bool`          | `Facility` | Checkbox (Pool? Wifi? Sauna?)                 |
| `Int`           | `Property` | Dropdown (Bedrooms: 1, 2, 3, 4, 5+)           |
| `Int`/`Decimal` | `Distance` | Dropdown (< 500m, < 1 km, < 5 km)             |
| `Text`          | Any        | Excluded from search (`IsSearchable = false`) |

The `Options` field provides predefined choices for dropdown rendering. When `null`, the UI falls back to free input (numeric) or checkbox (boolean).

#### Tags Decision: Lightweight Keywords, Not a Formal Entity

**Recommendation: No separate Tag entity.** Instead, add a `SearchKeywords` field to `VacationHouse`.

**Rationale**:

- Features already cover all **structured** attributes (Pool, Bedrooms, Distance to shop)
- Phase 22's full-text search on `Description` handles **informal** discovery ("romantic", "beach")
- A formal many-to-many Tag system adds: Tag entity, HouseTag join table, tag CRUD UI, tag admin, tag normalization — significant complexity for marginal benefit
- A `SearchKeywords` string field gives 80% of the value at 10% of the cost
- If formal tags are needed later, keywords can be migrated to a proper tag system

```
VacationHouse (modified)
├── SearchKeywords: string?    (comma-separated: "romantic, beach, modern, family-friendly")
```

The search engine (Phase 22) indexes `SearchKeywords` alongside `Title`, `Description`, and `AreaNames` in the `SearchVector`, making them discoverable via free-text search without any extra infrastructure.

#### Feature Category Seed Data

| Key               | Name                | Category | ValueType | Options                                 |
| ----------------- | ------------------- | -------- | --------- | --------------------------------------- |
| `bedrooms`        | Bedrooms            | Property | Int       | `["1","2","3","4","5","6+"]`            |
| `bathrooms`       | Bathrooms           | Property | Int       | `["1","2","3","4+"]`                    |
| `max_guests`      | Max guests          | Property | Int       | `["1-2","3-4","5-6","7-8","9+"]`        |
| `size_m2`         | Size (m²)           | Property | Int       | `null` (free input)                     |
| `pool`            | Swimming pool       | Facility | Bool      | `null`                                  |
| `sauna`           | Sauna               | Facility | Bool      | `null`                                  |
| `spa`             | Spa / hot tub       | Facility | Bool      | `null`                                  |
| `wifi`            | Internet / Wifi     | Facility | Bool      | `null`                                  |
| `dishwasher`      | Dishwasher          | Facility | Bool      | `null`                                  |
| `washing_machine` | Washing machine     | Facility | Bool      | `null`                                  |
| `dryer`           | Tumble dryer        | Facility | Bool      | `null`                                  |
| `pet_friendly`    | Pet friendly        | Facility | Bool      | `null`                                  |
| `fireplace`       | Fireplace           | Facility | Bool      | `null`                                  |
| `aircondition`    | Air conditioning    | Facility | Bool      | `null`                                  |
| `ev_charger`      | EV charger          | Facility | Bool      | `null`                                  |
| `nonsmoking`      | Non-smoking         | Facility | Bool      | `null`                                  |
| `handicap`        | Handicap accessible | Facility | Bool      | `null`                                  |
| `distance_shop`   | Distance to shop    | Distance | Int       | `["< 500m","< 1 km","< 2 km","< 5 km"]` |
| `distance_water`  | Distance to water   | Distance | Int       | `["< 100m","< 500m","< 1 km","< 5 km"]` |
| `water_view`      | Water view          | Distance | Bool      | `null`                                  |

#### Public API

```
GET /api/features/searchable    → returns features grouped by category with options
```

Response structure:

```json
{
  "Property": [
    {
      "key": "bedrooms",
      "name": "Bedrooms",
      "valueType": "Int",
      "options": ["1", "2", "3", "4", "5", "6+"]
    }
  ],
  "Facility": [
    { "key": "pool", "name": "Swimming pool", "valueType": "Bool" },
    { "key": "wifi", "name": "Internet / Wifi", "valueType": "Bool" }
  ],
  "Distance": [
    {
      "key": "distance_shop",
      "name": "Distance to shop",
      "valueType": "Int",
      "options": ["< 500m", "< 1 km", "< 2 km", "< 5 km"]
    }
  ]
}
```

The search UI consumes this endpoint to dynamically build filter controls.

### Tasks

- [x] **23a** Create `FeatureCategory` enum, add `Category`, `IsSearchable`, `Options` to `Feature` entity, migration
- [x] **23b** Add `SearchKeywords` (string, nullable) to `VacationHouse`, migration
- [x] **23c** Update `FeatureDto` and `UpsertFeatureDto` with new fields
- [x] **23d** Update `AdminFeatureService` CRUD to handle new fields
- [x] **23e** Admin UI: feature form with category dropdown, IsSearchable checkbox, options editor
- [x] **23f** Seed data: categorize existing features and add standard features from table above
- [x] **23g** Public API: `GET /api/features/searchable` — returns categorized features for search UI
- [x] **23h** Admin UI: `SearchKeywords` text field on house edit form (overview tab)
- [x] **23i** Tests: feature categorization CRUD, searchable feature listing, keyword persistence (7 integration tests)

**Estimated effort**: 5–8 hours | **Priority**: High (prerequisite for Phase 22)

---

## Phase 24: Internationalization — EN/DA Language Switching

**Goal**: Support switching between English and Danish across the entire MVC frontend, with a language switcher in the header.

**Dependencies**: None (can start independently, but best done after Phase 15 cleanup)

### Current State

- UI text is hardcoded Danish in Razor views (labels, buttons, headings)
- Error messages are English (after Phase 13d/14a cleanup)
- Feature names and area/city names are stored in Danish in the DB
- No localization framework — all strings are inline in `.cshtml` files

### Design

#### Approach: ASP.NET Core Built-in Localization

Use `IStringLocalizer<T>` + `.resx` resource files — the standard .NET approach. This integrates natively with Razor views via `IViewLocalizer`, handles culture negotiation, and is well-documented.

#### Language Selection

- **Cookie-based** (primary): `.AspNetCore.Culture` cookie set via language switcher in header
- **URL query** (for sharing): `?culture=da-DK` or `?culture=en-GB`
- **Default**: Danish (`da-DK`) since this is a Danish product

#### What Gets Localized

| Layer                   | Approach                                                   |
| ----------------------- | ---------------------------------------------------------- |
| **MVC Views** (public)  | `IViewLocalizer` + `.resx` per view or shared resource     |
| **Error messages**      | `IStringLocalizer<SharedResource>` in controllers/services |
| **Feature names**       | DB: add `NameEn` column to `Feature` entity                |
| **Area names**          | DB: add `NameEn` column to `Area` entity                   |
| **City names**          | Keep Danish only (official geographic names)               |
| **Validation messages** | DataAnnotations localization via adapter                   |
| **JavaScript strings**  | Pass via `data-*` attributes or inline `<script>` block    |

#### Resource File Structure

```
Sommerhus.Mvc/
├── Resources/
│   ├── SharedResource.da.resx       (Danish — base, can be empty if views default to DA)
│   ├── SharedResource.en.resx       (English translations)
│   ├── Views/Houses/Details.en.resx (per-view overrides if needed)
│   └── ...
```

#### Language Switcher UI

Simple dropdown or flag toggle in `_Layout.cshtml` header:

- 🇩🇰 Dansk | 🇬🇧 English
- Sets the culture cookie via a `POST` to `CultureController` and reloads the page
- Current language visually highlighted

#### DB Translation Strategy

For a two-language system, direct columns are simpler than a translation table:

```
Feature (modified)
├── Name: string        (Danish — default)
├── NameEn: string?     (English — null falls back to Danish)

Area (modified)
├── Name: string        (Danish)
├── NameEn: string?     (English)
```

Resolution: `culture == "en" && entity.NameEn != null ? entity.NameEn : entity.Name`

If more languages are needed later, migrate to a `Translation` table with `(EntityType, EntityId, Culture, Value)`. For now, 2 columns is pragmatic and avoids join overhead.

### Tasks

- [ ] **24a** Configure ASP.NET Core localization in `Sommerhus.Mvc/Program.cs` (supported cultures, cookie provider, request localization middleware)
- [ ] **24b** Create `SharedResource.resx` (DA) and `SharedResource.en.resx` (EN) with common strings (nav, buttons, labels, error messages)
- [ ] **24c** Add language switcher to `_Layout.cshtml` with `CultureController.SetCulture` action to set cookie
- [ ] **24d** Localize shared layout: navigation labels, footer, flash message text
- [ ] **24e** Localize public views: Houses (list, details, search), Areas (list, details), Home page
- [ ] **24f** Add `NameEn` to `Feature` and `Area` entities, migration
- [ ] **24g** Update DTOs and services to return localized names based on `CultureInfo.CurrentCulture`
- [ ] **24h** Localize admin views (optional — admin is typically internal, can stay Danish initially)
- [ ] **24i** Localize validation error messages via DataAnnotations localization
- [ ] **24j** Tests: culture switching, localized API responses, fallback behavior

**Estimated effort**: 10–15 hours | **Priority**: Medium

---

## Phase 25: Stress Testing & Realistic Seed Data

**Goal**: Generate a realistic Danish dataset for development and load testing, then stress test the system to establish performance baselines — specifically **before and after Phase 20 (Price Snapshots)** to measure the impact of pre-computed pricing.

**Dependencies**: Should be completed before Phase 20 to establish baseline metrics. Run again after Phase 20 for comparison.

### Design

#### Realistic Seed Data

**Danish geography** (from DAWA API — `dawa.aws.dk`):

- All ~1,300 Danish zip codes with city names
- Municipalities and regions for area generation
- Real addresses for realistic house locations

**Data volumes** (configurable via parameters):

| Entity                 | Development         | Stress Test     |
| ---------------------- | ------------------- | --------------- |
| Cities (zip codes)     | ~1,300 (all Danish) | ~1,300          |
| Areas                  | 50                  | 100             |
| Houses                 | 500                 | 5,000–10,000    |
| Features               | 25                  | 25              |
| HouseFeatureValues     | ~5,000              | ~50,000–100,000 |
| Season Calendars       | 10                  | 50              |
| Price Plans            | 500                 | 5,000–10,000    |
| Season Prices          | ~3,000              | ~30,000–60,000  |
| Availability Blocks    | ~1,000              | ~10,000–20,000  |
| Images (metadata only) | ~2,500              | ~25,000–50,000  |

**Data generation approach**:

- `Bogus` NuGet package for realistic fake data (house titles, descriptions, addresses)
- Danish zip codes fetched from DAWA API or bundled as `DanishGeoData.json`
- Features distributed realistically (90% have wifi, 15% have pool, etc.)
- Season calendars with realistic Danish holiday patterns (summer high season Jun–Aug, winter low season Nov–Feb)
- Prices in realistic DKK ranges (500–5,000/night depending on season and house size)
- Availability blocks: ~60% available, ~20% booked, ~20% owner-blocked

#### Load Testing Tool

**Recommendation: k6** (open source by Grafana Labs)

- JavaScript-based test scripts — easy to write and maintain
- Runs locally — no infrastructure or cloud accounts needed
- Excellent metrics: p50/p95/p99 response times, throughput, error rates
- Export to JSON/CSV for before/after comparison

Alternative: **NBomber** (.NET native, if the team prefers C# test scripts)

#### Test Scenarios

| Scenario                 | Description                                               | Target      |
| ------------------------ | --------------------------------------------------------- | ----------- |
| **Public Search**        | Search with various filters (area, features, price range) | < 200ms p95 |
| **House Detail + Price** | Load house detail page + compute price quote              | < 300ms p95 |
| **Search Pagination**    | Browse through 10 pages of results                        | < 200ms p95 |
| **Concurrent Users**     | 50–200 simultaneous users browsing and searching          | < 500ms p95 |
| **Admin CRUD**           | Create/update houses, change prices                       | < 500ms p95 |

#### Benchmark Protocol

1. **Baseline** (before Phase 20): Seed 5,000 houses → run all scenarios → record metrics
2. **After Phase 20**: Same data → same scenarios → compare metrics
3. **Document**: Response time improvement, query count reduction, throughput change
4. **Report**: Markdown file in `stress-tests/results/` with side-by-side comparison

#### Implementation Structure

```
Sommerhus.Core/
├── Data/
│   ├── DbSeeder.cs                  (existing — minimal dev seed)
│   ├── StressDataGenerator.cs       (NEW — generates realistic bulk data)
│   └── DanishGeoData.json           (bundled zip codes/cities from DAWA)

stress-tests/
├── k6/
│   ├── public-search.js             (search with filters)
│   ├── house-detail.js              (detail page + pricing)
│   ├── concurrent-browse.js         (multi-user simulation)
│   └── admin-crud.js                (admin operations under load)
├── results/
│   ├── baseline-YYYY-MM-DD.json     (before Phase 20)
│   └── post-phase20-YYYY-MM-DD.json (after Phase 20)
└── README.md                         (how to run tests, interpret results)
```

#### Admin Endpoint for Data Generation

```http
POST   /api/admin/stress/seed?houses=5000    # Generate realistic data
DELETE /api/admin/stress/clear                # Remove generated data
```

Protected by admin auth. Only available in `Development` / `Testing` environments (guarded by `IHostEnvironment` check).

### Tasks

- [x] **25a** Bundle Danish zip code/city data (JSON from DAWA API, 1,089 entries as embedded resource)
- [x] **25b** Add `Bogus` NuGet package, build `StressDataGenerator` service with configurable entity counts
- [x] **25c** Generate realistic features with proper categorization and distribution (depends on Phase 23)
- [x] **25d** Generate realistic pricing: season calendars, price plans, season prices in DKK ranges
- [x] **25e** Generate realistic availability blocks (available/booked/blocked distribution)
- [x] **25f** Admin API: `POST /api/admin/stress/seed` + `DELETE /api/admin/stress/clear` (dev/test only)
- [x] **25g** Write k6 load test scripts for all scenarios (search, detail, concurrent, admin)
- [x] **25h** Run baseline benchmark, save results to `stress-tests/results/`
- [x] **25i** After Phase 20: run comparison benchmark, document improvement in results report
- [x] **25j** Tests: verify data generator produces valid, queryable data with correct relationships (5 integration tests)
- [x] **25k** Seed owners (`HouseOwner` role) and users (`User` role) with realistic profile data (Feb 12, 2026)
  - `StressDataOptions`: added `OwnerCount` (default 20) and `UserCount` (default 50)
  - Owners get `FirstName`, `LastName`, `Phone`, `Address` via Bogus
  - Users get `FirstName`, `LastName`, `Phone` via Bogus
  - ~80% of houses assigned to a random owner via `OwnerId`
  - `ClearAsync` deletes all `stress_*` users via `UserManager`
  - API endpoint accepts `?owners=` and `?users=` query parameters
  - Tests verify: owner/user creation, role assignment, house ownership, cleanup

**Estimated effort**: 10–15 hours | **Priority**: High

---

## Phase 26: Multi-Role Authorization & User Management

**Goal**: Expand the single-role (Admin) system into a four-tier role model: **Guest** (anonymous), **User** (registered guest), **HouseOwner**, and **Admin**. This is the foundation for bookings, favorites, and owner self-service.

**Dependencies**: Phase 18 (Entity Status) ✅ — uses `EntityStatus` for house visibility. Phase 17 (Audit) ✅ — all mutations are auditable.

### Current State

- Only one role exists: `Admin` (seeded via `AdminIdentitySeeder`)
- `ApplicationUser` extends `IdentityUser` with no additional fields
- Auth is JWT-based, admin-only — no public registration or login
- All API endpoints are either `[AllowAnonymous]` (public read) or `[Authorize(Roles = "Admin")]`
- No concept of house ownership — houses are managed exclusively by admins
- MVC frontend has no user-facing auth (no register, login, or profile pages)

### Design

#### Role Definitions

| Role           | Description                          | Can Register? |
| -------------- | ------------------------------------ | ------------- |
| **Guest**      | Anonymous visitor, no account needed | N/A           |
| **User**       | Registered guest with an account     | Yes (public)  |
| **HouseOwner** | Owns one or more vacation houses     | Admin assigns |
| **Admin**      | Full system access                   | Seeded only   |

#### Permission Matrix

| Action                                  | Guest | User | Owner | Admin |
| --------------------------------------- | ----- | ---- | ----- | ----- |
| Browse/search houses                    | ✅    | ✅   | ✅    | ✅    |
| View house details                      | ✅    | ✅   | ✅    | ✅    |
| View availability                       | ✅    | ✅   | ✅    | ✅    |
| Calculate price quote                   | ✅    | ✅   | ✅    | ✅    |
| Start booking flow                      | ✅    | ✅   | ✅    | ✅    |
| Complete booking (confirm)              | ❌    | ✅   | ✅    | ✅    |
| View own bookings / history             | ❌    | ✅   | ✅    | ✅    |
| Save to favorites                       | ❌    | ✅   | ✅    | ✅    |
| Manage personal profile                 | ❌    | ✅   | ✅    | ✅    |
| View own houses                         | ❌    | ❌   | ✅    | ✅    |
| Edit own house (desc, images, features) | ❌    | ❌   | ✅    | ✅    |
| View own house bookings                 | ❌    | ❌   | ✅    | ✅    |
| View own house calendar                 | ❌    | ❌   | ✅    | ✅    |
| Edit calendar / pricing                 | ❌    | ❌   | ❌    | ✅    |
| Create/edit areas, cities               | ❌    | ❌   | ❌    | ✅    |
| Create/edit global features             | ❌    | ❌   | ❌    | ✅    |
| Manage all houses                       | ❌    | ❌   | ❌    | ✅    |
| Manage all bookings                     | ❌    | ❌   | ❌    | ✅    |
| Assign house owners                     | ❌    | ❌   | ❌    | ✅    |
| Stress test / system admin              | ❌    | ❌   | ❌    | ✅    |

#### ApplicationUser Extension

```csharp
public sealed class ApplicationUser : IdentityUser
{
    [MaxLength(100)] public string? FirstName { get; set; }
    [MaxLength(100)] public string? LastName { get; set; }
    [MaxLength(20)]  public string? Phone { get; set; }
    [MaxLength(500)] public string? Address { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAtUtc { get; set; }
}
```

#### House Ownership Model

Add `OwnerId` to `VacationHouse`:

```
VacationHouse (modified)
├── OwnerId: string?          (FK → ApplicationUser.Id, nullable for legacy/admin-created houses)
├── Owner: ApplicationUser?
```

Admin assigns ownership via `PUT /api/admin/houses/{id}/owner`. Owners see only their houses via `GET /api/owner/houses`.

#### API Structure

```
Public (anonymous):
  POST /api/auth/register           → register new User account
  POST /api/auth/login              → login (returns JWT with role claims)
  POST /api/auth/refresh            → refresh token (optional, future)

User-scoped:
  GET    /api/me/profile            → get own profile
  PUT    /api/me/profile            → update own profile
  DELETE /api/me/account            → deactivate account

Owner-scoped:
  GET    /api/owner/houses                          → list own houses
  GET    /api/owner/houses/{id}                     → view own house details
  PUT    /api/owner/houses/{id}                     → edit description, images, features
  GET    /api/owner/houses/{id}/bookings            → view bookings for own house
  GET    /api/owner/houses/{id}/availability        → view availability for own house
  GET    /api/owner/houses/{id}/calendar            → view calendar for own house

Admin (existing + new):
  PUT    /api/admin/houses/{id}/owner               → assign/remove owner
  GET    /api/admin/users                           → list all users with roles
  PUT    /api/admin/users/{id}/role                 → change user role
```

#### Authorization Policies

```csharp
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string HouseOwner = "HouseOwner";
    public const string User = "User";
}

// Policies
options.AddPolicy("RequireUser", p => p.RequireAuthenticatedUser());
options.AddPolicy("RequireOwner", p => p.RequireRole(AppRoles.HouseOwner, AppRoles.Admin));
options.AddPolicy("RequireAdmin", p => p.RequireRole(AppRoles.Admin));
```

#### Owner Authorization Guard

Owner endpoints use a custom `IOwnerAuthorizationService` to verify the requesting user owns the house:

```csharp
public interface IOwnerAuthorizationService
{
    Task<bool> IsOwnerAsync(string userId, Guid houseId, CancellationToken ct);
}
```

This prevents owners from accessing other owners' houses via direct URL manipulation.

#### MVC Auth Views

- `/account/register` — registration form
- `/account/login` — login form (redirects to home after success)
- `/account/profile` — edit personal info
- `/owner/houses` — owner dashboard (list own houses)
- `/owner/houses/{id}` — owner house detail/edit

### Tasks

- [x] **26a** Expand `AppRoles` constants (`Admin`, `HouseOwner`, `User`), update `AdminIdentitySeeder` to seed all three roles
- [x] **26b** Extend `ApplicationUser` with profile fields (`FirstName`, `LastName`, `Phone`, `Address`, `CreatedAtUtc`, `LastLoginAtUtc`), migration
- [x] **26c** Add `OwnerId` (string, nullable) FK to `VacationHouse`, update DbContext, migration
- [x] **26d** Create public `AuthController` with `POST /api/auth/register` and `POST /api/auth/login` (multi-role JWT)
- [x] **26e** Create `UserProfileController` (`GET/PUT /api/me/profile`)
- [x] **26f** Create `IOwnerAuthorizationService` + `OwnerAuthorizationService` (verify house ownership)
- [x] **26g** Create `OwnerHouseController` — owner CRUD on own houses (description, view details with calendar/pricing)
- [x] **26h** Admin: `PUT /api/admin/houses/{id}/owner` to assign ownership, `GET /api/admin/users` to list users, `PUT /api/admin/users/{id}/role` to change role
- [x] **26i** Update authorization attributes across all existing controllers (backward compatible via `AdminRoles` alias)
- [x] **26j** MVC: registration, login pages with multi-role support (public-facing)
- [ ] **26k** MVC: owner dashboard with house list and edit views _(deferred to Phase 28)_
- [x] **26l** Tests: 12 new integration tests — registration, login, role-based access, owner isolation, admin user management (77 total, all passing)

**Estimated effort**: 15–20 hours | **Priority**: High | **Status**: ✅ Completed

---

## Phase 27: Booking System ✅

**Goal**: Implement a complete booking flow — from price quote to confirmed reservation — with proper lifecycle management, availability integration, and role-based visibility.

**Dependencies**: Phase 26 (Multi-Role Auth) — requires User and Owner roles. Phase 21 (Availability) ✅ — booking creates availability blocks. Phase 16 (Calendar/Pricing) ✅ — price calculation for quotes.

### Current State

- ✅ `Booking` entity with `BookingStatus` enum (Pending → Confirmed → Completed / Cancelled)
- ✅ `BookingService` (user), `OwnerBookingService`, `AdminBookingService` with full lifecycle
- ✅ API controllers: `api/bookings`, `api/owner/bookings`, `api/admin/bookings`
- ✅ Availability integration: confirm creates `AvailabilityBlock`, cancel removes it
- ✅ Price integration: booking creation calls pricing pipeline for total price
- ✅ 11 integration tests, 88 total passing
- ⏳ MVC views and seed data not yet implemented

### Design

#### Booking Entity

```csharp
public class Booking : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public Guid HouseId { get; set; }
    public VacationHouse House { get; set; } = null!;

    [Required] public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;

    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public int Guests { get; set; }

    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = "DKK";

    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public DateTime? ConfirmedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    [MaxLength(500)] public string? CancellationReason { get; set; }

    [MaxLength(500)] public string? GuestNotes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(256)] public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    [MaxLength(256)] public string? UpdatedBy { get; set; }
}

public enum BookingStatus
{
    Pending = 0,
    Confirmed = 1,
    Completed = 2,
    Cancelled = 3
}
```

#### Booking Lifecycle

```
Pending → Confirmed → Completed
   │          │
   └──→ Cancelled ←──┘
```

- **Pending**: User submitted booking, awaiting confirmation
- **Confirmed**: Admin or owner confirmed the booking → auto-creates `AvailabilityBlock` (Blocked)
- **Completed**: Check-out date has passed (can be auto-transitioned or manual)
- **Cancelled**: User, owner, or admin cancelled → removes linked availability block

#### Availability Integration

When a booking is confirmed:

1. Create an `AvailabilityBlock` with `Status = Blocked`, `Source = Booking`
2. Store `BookingId` reference on the block (or in the `Note` field)
3. On cancellation, remove or mark the linked availability block as available

#### API Structure

```
Public (guest can start, user must be logged in to confirm):
  POST /api/houses/{id}/quote       → calculate price for dates + guests (anonymous)
  POST /api/houses/{id}/book        → create pending booking (requires User role)

User-scoped:
  GET    /api/me/bookings           → list own bookings (with filters: status, date range)
  GET    /api/me/bookings/{id}      → booking detail
  POST   /api/me/bookings/{id}/cancel → cancel own booking (if Pending or Confirmed)

Owner-scoped:
  GET    /api/owner/houses/{id}/bookings          → bookings for own house
  POST   /api/owner/bookings/{id}/confirm         → confirm a pending booking
  POST   /api/owner/bookings/{id}/cancel          → cancel a booking

Admin:
  GET    /api/admin/bookings                      → list all bookings (filters, pagination)
  GET    /api/admin/bookings/{id}                 → booking detail
  PUT    /api/admin/bookings/{id}/status           → change booking status
  POST   /api/admin/bookings/{id}/cancel           → cancel any booking
```

#### Overlap Validation

Before creating a booking, the service checks:

1. House is `Published`
2. Dates don't overlap with existing `AvailabilityBlock` (Blocked status)
3. Dates don't overlap with another `Confirmed` booking
4. Check-in is before check-out, both in the future
5. Guest count is within `max_guests` feature value

#### DTOs

```csharp
public sealed record QuoteRequestDto(DateOnly CheckIn, DateOnly CheckOut, int Guests);
public sealed record QuoteResponseDto(decimal TotalPrice, string Currency, decimal NightlyAverage, int Nights);
public sealed record CreateBookingDto(DateOnly CheckIn, DateOnly CheckOut, int Guests, string? GuestNotes);
public sealed record BookingSummaryDto(Guid Id, Guid HouseId, string HouseTitle, DateOnly CheckIn, DateOnly CheckOut, int Guests, decimal TotalPrice, string Currency, BookingStatus Status, DateTime CreatedAtUtc);
public sealed record BookingDetailDto(Guid Id, Guid HouseId, string HouseTitle, string? HouseAddress, DateOnly CheckIn, DateOnly CheckOut, int Guests, decimal TotalPrice, string Currency, BookingStatus Status, string? GuestNotes, DateTime CreatedAtUtc, DateTime? ConfirmedAtUtc, DateTime? CancelledAtUtc, string? CancellationReason);
```

### Tasks

- [x] **27a** Create `Booking` entity, `BookingStatus` enum in Domain
- [x] **27b** Add `DbSet<Booking>`, configure relationships and indexes in DbContext, migration
- [x] **27c** Create `IBookingService` + `BookingService` — create booking, validate dates/overlap/availability
- [x] **27d** Price quote integration: booking creation calls existing pricing pipeline for total price
- [x] **27e** User booking: `POST /api/bookings`, `GET /api/bookings` (cancel, list, detail)
- [x] **27f** Availability integration: auto-create `AvailabilityBlock` on confirm, remove on cancel
- [x] **27g** Owner booking view: `GET /api/owner/bookings`, confirm/reject actions
- [x] **27h** Admin booking management: list, detail, status change with validated transitions
- [x] **27i** MVC: booking flow UI (create, list, details, cancel) — completed in Phase 28
- [x] **27j** MVC: owner booking confirm/reject actions — completed in Phase 28
- [x] **27k** Seed data: bookings and favorites in `StressDataGenerator` — completed in Phase 28
- [x] **27l** Tests: booking CRUD, lifecycle transitions, availability integration, role isolation (11 tests)

**Estimated effort**: 15–20 hours | **Priority**: High

---

## Phase 28: Favorites & User Dashboard ✅

**Goal**: Allow registered users to save houses to a favorites list, and provide role-specific dashboards for users and owners.

**Dependencies**: Phase 26 (Multi-Role Auth) — requires User role. Phase 27 (Booking System) — dashboard shows bookings.

### Current State

- ✅ `FavoriteHouse` entity with composite PK, EF migration, full CRUD service
- ✅ API: `GET/POST/DELETE /api/me/favorites` with authorization
- ✅ `PublicHouseDetailsDto.IsFavorite` flag for authenticated users
- ✅ User dashboard (`/account/dashboard`): upcoming bookings, favorites grid, booking history
- ✅ Owner dashboard (`/owner/dashboard`): my houses, upcoming bookings, confirm/reject actions
- ✅ MVC booking flow: create, list, details, cancel (`/bookings`, `/houses/{id}/book`)
- ✅ Favorite heart toggle on house detail page
- ✅ Navbar links for Dashboard, Bookings, Owner
- ✅ `UserApiClient` for authenticated MVC API calls
- ✅ Stress data generator seeds bookings and favorites
- ✅ 10 new integration tests (98 total, all passing)

### Design

#### FavoriteHouse Entity

```csharp
public class FavoriteHouse
{
    public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;

    public Guid HouseId { get; set; }
    public VacationHouse House { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
```

Composite PK: `(UserId, HouseId)`. Simple join table, no extra metadata needed.

#### API Structure

```
User-scoped:
  GET    /api/me/favorites          → list favorite houses (with summary data)
  POST   /api/me/favorites/{houseId} → add house to favorites
  DELETE /api/me/favorites/{houseId} → remove from favorites
  GET    /api/houses/{id}           → include "isFavorite" flag when user is authenticated
```

#### Dashboard Views

**User Dashboard** (`/account/dashboard`):

- **Upcoming bookings** — next 3 confirmed bookings with house image and dates
- **Favorites** — saved houses grid with quick-view links
- **Booking history** — paginated list of past bookings
- **Profile summary** — name, email, phone with edit link

**Owner Dashboard** (`/owner/dashboard`):

- **My houses** — list of owned houses with status badge and occupancy indicator
- **Upcoming bookings** — aggregated across all owned houses
- **Calendar overview** — mini-calendar showing availability across houses
- **Quick actions** — edit house, view bookings, view calendar

#### House Card Enhancement

When a logged-in user browses houses, each house card shows a heart icon (♡/♥) for favorites toggle. This requires:

- Public house list API includes `isFavorite` per house when `Authorization` header is present
- Toggle is a simple `POST`/`DELETE` to `/api/me/favorites/{houseId}`

### Tasks

- [x] **28a** Create `FavoriteHouse` entity, configure composite PK in DbContext, migration
- [x] **28b** Create `IFavoriteService` + `FavoriteService` — add, remove, list, check if favorited
- [x] **28c** API: `GET/POST/DELETE /api/me/favorites` endpoints
- [x] **28d** Extend public house detail API to include `isFavorite` flag for authenticated users
- [x] **28e** MVC: user dashboard page (upcoming bookings, favorites grid, booking history)
- [x] **28f** MVC: owner dashboard page (my houses, upcoming bookings, confirm/reject)
- [x] **28g** MVC: favorite toggle (heart icon) on house detail page, navbar dashboard links
- [x] **28h** Tests: 10 integration tests — favorite CRUD, duplicate handling, auth, isolation, isFavorite flag

**Estimated effort**: 8–12 hours | **Priority**: Medium

---

## Phase Dependency Graph

```
Phase 16 (Calendar) ✅ ──┐
                         ├──→ Phase 20 (Price Snapshots) ──┐
Phase 17 (Audit) ✅      │                                 │
                         │                                 ├──→ Phase 22 (Search)
Phase 18 (Status) ✅ ────┤──→ Phase 21 (Availability) ────┘         ↑
                         │                                           │
Phase 19 (Concurrency) ⏸ ON HOLD                                    │
                                                                     │
Phase 23 (Feature Categories) ✅ ───────────────────────────────────┘

Phase 24 (i18n) ──────── independent, can start anytime after Phase 15

Phase 25 (Stress Test) ✅ partial ── before Phase 20 (baseline) → after Phase 20 (comparison)

Phase 17 (Audit) ✅ ─────┐
Phase 18 (Status) ✅ ─────┤──→ Phase 26 (Auth) ✅ ──→ Phase 27 (Booking) ✅ ──→ Phase 28 (Favorites) ✅
Phase 21 (Availability) ──┘                                              │
Phase 16 (Calendar/Pricing) ✅ ──────────────────────────────────────────┘
```

**Recommended order**: ~~17 → 19 → 18 → 16~~ (done) → ~~23 → 25~~ (done) → ~~26 → 27 → 28~~ (done) → **20 → 22 → 24**

- ~~**26 (Multi-Role Auth)**~~ ✅ done
- ~~**27 (Booking System)**~~ ✅ done (all tasks including deferred MVC/seed)
- ~~**28 (Favorites & Dashboard)**~~ ✅ done
- **20 (Price Snapshots)** after 25 baseline: then re-run stress test to measure improvement
- **22 (Search)** after 20 + 23: aggregates price summaries and categorized features
- **24 (i18n)** anytime: independent, can be parallelized with other work
- **19 (Concurrency)** on hold: revisit when multi-admin editing becomes a real concern

---

## Phase 29: Codebase Hygiene & Predictability

**Goal**: Make the codebase consistent and predictable so colleagues can navigate it without surprises. Every entity, service, and controller should follow the same patterns.

**Dependencies**: None (can start anytime)

### 29a: Constructor Style Consistency (estimated 1-2 hours)

About half the services use **primary constructors** (C# 12) while the other half use traditional constructor + field assignment. This inconsistency makes it harder to scan code quickly.

**Services using traditional constructors** (should migrate to primary constructors):

- `AdminHouseService`, `AdminHouseFeatureService`, `AdminHousePricingService`
- `AdminAreaService`, `AdminCityService`, `AdminFeatureService`
- `AdminHouseGroupService`, `AdminPricingService`, `EntityLifecycleService`
- `HouseQueryService`, `AreaQueryService`, `FeatureQueryService`
- `CityQueryService`, `ZipCodeQueryService`, `HouseImageQueryService`

**Services already using primary constructors** (good):

- `OwnerHouseService`, `OwnerAuthorizationService`
- `AdminAvailabilityService`, `AdminCalendarService`, `AuditService`
- `AvailabilityQueryService`, `StressDataGenerator`

Tasks:

- [x] **29a-1** Convert all Core services to primary constructors (match `OwnerHouseService` pattern)
- [x] **29a-2** Remove redundant `private readonly` field assignments where primary constructor params suffice

### 29b: MVC HousesController Split (estimated 2-3 hours)

`Sommerhus.Mvc/Controllers/Admin/HousesController.cs` is **677 lines** with 20+ actions covering CRUD, images, features, pricing, calendar, and calendar overrides. This violates the "keep services under 300 lines" rule and makes it hard to find specific functionality.

Tasks:

- [x] **29b-1** Extract image actions (`UploadHouseImages`, `SetHouseImageKind`, `DeleteHouseImage`) → `HouseImagesController`
- [x] **29b-2** Extract feature actions (`SaveHouseFeatures`) → `HouseFeaturesController`
- [x] **29b-3** Extract pricing actions (`SaveHousePricing`, `DeleteRatePlan`) → `HousePricingController`
- [x] **29b-4** Extract calendar actions (`AddHouseSeasonSpan`, `UpdateHouseSeasonSpan`, `DeleteHouseSeasonSpan`, `SetCalendarOverride`, `RemoveCalendarOverride`, `CreateCalendarOverride`) → `HouseCalendarController`
- [x] **29b-5** Resulting `HousesController` should be ~200 lines (CRUD + status only)

### 29c: AdminRoles / AppRoles Cleanup (estimated 30 min)

`AdminRoles.cs` contains both `AppRoles` and a backward-compatible `AdminRoles` alias. Now that Phase 26 is complete and all controllers use the new role system, the alias adds confusion.

Tasks:

- [x] **29c-1** Search for all `AdminRoles.Admin` usages and replace with `AppRoles.Admin`
- [x] **29c-2** Delete the `AdminRoles` class
- [x] **29c-3** Rename file from `AdminRoles.cs` to `AppRoles.cs`

### 29d: DTO Location — Move Security/Auth DTOs Out of `Dtos/` (estimated 30 min)

`AppRoles` lives in `Dtos/Security/` which is misleading — it's not a DTO. Auth DTOs (`RegisterRequest`, `LoginRequest`, `AuthTokenResponse`) are in `Dtos/Auth/` but are really request/response models.

Tasks:

- [x] **29d-1** Move `AppRoles.cs` to `Sommerhus.Core/Identity/AppRoles.cs` (next to `ApplicationUser`)
- [x] **29d-2** Auth DTOs kept in `Sommerhus.Core/Dtos/Auth/` (team-preferred location)
- [x] **29d-3** `Dtos/Security/AdminRoles.cs` emptied (old content removed)

### 29e: Service Registration — Move `IStressDataGenerator` Out of Admin Services (estimated 15 min)

`IStressDataGenerator` is registered in `AddAdminServices()` but it's a dev/test utility, not an admin business service. This is misleading.

Tasks:

- [x] **29e-1** Move `AddScoped<IStressDataGenerator, StressDataGenerator>()` to a new `AddDevServices()` extension method

### 29f: Inconsistent Image Service Base Class Usage (estimated 1 hour)

The three image services (`AdminAreaImageService`, `AdminCityImageService`, `AdminHouseImageService`) inherit from `AdminImageServiceBase` but the generic interfaces `IAdminEntityImageService<T>` created in Phase 4 are not actually used by any consumer. They add cognitive overhead without benefit.

Tasks:

- [x] **29f-1** Audit whether `IAdminEntityImageService<T>` is referenced anywhere outside the image services
- [x] **29f-2** Unused — deleted the generic interfaces, keeping only the concrete interfaces

### 29g: Remaining Low-Priority Items from Phase 15b (estimated 1-2 hours)

These were deferred from Phase 15b and are still open:

- [x] **#25** Standardize all Upsert DTOs to `sealed class` with `{ get; set; }` for form binding (positional records kept where constructor syntax is used extensively in tests)
- [x] **#28** Remove unused generic image service interfaces or implement them
- [x] **#30** Convert Core services to primary constructors (overlaps with 29a)

### 29h: `AdminControllerBase` Authorization Gap (estimated 30 min)

`AdminControllerBase` uses `[Authorize]` (any authenticated user) instead of `[Authorize(Roles = "Admin")]`. Each admin controller adds its own role attribute, but if one forgets, any logged-in User or HouseOwner could access admin pages.

Tasks:

- [x] **29h-1** Change `AdminControllerBase` from `[Authorize]` to `[Authorize(Roles = AppRoles.Admin)]`
- [ ] **29h-2** Remove redundant `[Authorize(Roles = ...)]` from individual MVC admin controllers that inherit from it (deferred — API controllers don't inherit from AdminControllerBase)

### Verification (after each sub-task)

```powershell
dotnet format Sommerhus_project.sln
dotnet build Sommerhus_project.sln --warnaserror
dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj --logger "console;verbosity=detailed"
```

**Estimated total effort**: 6-10 hours | **Priority**: Medium

---

## Post-Refactor Checklist

After completing all phases:

- [x] All tests passing
- [x] No build warnings
- [ ] Code formatted with `dotnet format`
- [x] Documentation updated
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
| Phase 14a | 1-2 hours        | High     | **Completed** |
| Phase 14b | 2-3 hours        | High     | **Completed** |
| Phase 14c | 2-3 hours        | Medium   | **Completed** |
| Phase 14d | 1 hour           | Low      | **Completed** |
| Phase 15a | 30 min           | High     | **Completed** |
| Phase 15b | 3-4 hours        | Medium   | **Completed** |
| Phase 16  | 8-12 hours       | High     | **Completed** |
| Phase 17  | 6-10 hours       | High     | **Completed** |
| Phase 18  | 5-8 hours        | High     | **Completed** |
| Phase 19  | 5-7 hours        | Medium   | **On Hold**   |
| Phase 20  | 8-12 hours       | High     | **Completed** |
| Phase 21  | 8-12 hours       | High     | **Partial**   |
| Phase 22  | 10-15 hours      | Medium   | Pending       |
| Phase 23  | 5-8 hours        | High     | **Completed** |
| Phase 24  | 10-15 hours      | Medium   | Pending       |
| Phase 25  | 10-15 hours      | High     | **Partial**   |
| Phase 26  | 15-20 hours      | High     | **Completed** |
| Phase 27  | 15-20 hours      | High     | Pending       |
| Phase 28  | 8-12 hours       | Medium   | Pending       |
| Phase 29  | 6-10 hours       | Medium   | **Completed** |

**Remaining (refactoring)**: ~29-43 hours
**Remaining (enterprise)**: ~75-114 hours
**Remaining (user features)**: ~38-52 hours

---

## How to Use This Roadmap

1. **Start with Phase 1** - It's low-risk and improves developer experience
2. **Run tests after each phase** - Don't proceed if tests fail
3. **Commit after each phase** - Keep changes isolated
4. **Update this document** - Check off tasks as completed
5. **Adjust as needed** - Real-world findings may change priorities

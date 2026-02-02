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

## Phase 4: Image Handling Consistency

**Goal**: Unify image handling patterns across House/Area/City.

### Tasks

- [ ] Review current image service implementations
- [ ] Create generic `IEntityImageService<TEntity>` interface
- [ ] Refactor `AdminHouseImageService`, `AdminAreaImageService`, `AdminCityImageService`
- [ ] Consolidate duplicate code into shared base class
- [ ] Ensure consistent URL generation

### Files to Create

- `Sommerhus.Application/Admin/Images/IEntityImageService.cs`
- `Sommerhus.Repository/Admin/Images/EntityImageService.cs` (base)

### Files to Modify

- Existing image services (3 files)
- Image controllers

### Verification

```powershell
dotnet test --filter "FullyQualifiedName~Images"
# Manual: Upload images via admin UI for houses, areas, cities
```

---

## Phase 5: Remove Sommerhus.Pricing Project

**Goal**: Clean up unused project or implement pricing engine.

### Decision Point

**Option A**: Delete empty `Sommerhus.Pricing/` project
**Option B**: Move pricing engine from `Sommerhus.Application/Pricing/` to dedicated project

### Tasks (Option A)

- [ ] Remove `Sommerhus.Pricing.csproj` from solution
- [ ] Delete `Sommerhus.Pricing/` folder
- [ ] Verify no broken references

### Tasks (Option B)

- [ ] Move `Application/Pricing/Engine/` to `Sommerhus.Pricing/`
- [ ] Update project references
- [ ] Update namespaces

### Verification

```powershell
dotnet build Sommerhus_project.sln
dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj
```

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

| Phase    | Estimated Effort | Priority |
| -------- | ---------------- | -------- |
| Phase 1  | 1-2 hours        | High     |
| Phase 2  | 2-3 hours        | High     |
| Phase 3  | 2-3 hours        | Medium   |
| Phase 4  | 2-3 hours        | Medium   |
| Phase 5  | 30 min           | Low      |
| Phase 6  | 1-2 hours        | Medium   |
| Phase 7  | 2-3 hours        | Low      |
| Phase 8  | 3-4 hours        | High     |
| Phase 9  | 2-3 hours        | Medium   |
| Phase 10 | 2-3 hours        | Medium   |

**Total**: ~18-26 hours

---

## How to Use This Roadmap

1. **Start with Phase 1** - It's low-risk and improves developer experience
2. **Run tests after each phase** - Don't proceed if tests fail
3. **Commit after each phase** - Keep changes isolated
4. **Update this document** - Check off tasks as completed
5. **Adjust as needed** - Real-world findings may change priorities

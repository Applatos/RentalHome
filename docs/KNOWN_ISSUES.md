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

| Issue                           | Priority | Phase | Status        |
| ------------------------------- | -------- | ----- | ------------- |
| #1 Service Registration         | High     | 1     | **Completed** |
| #2 Large Services               | High     | 2     | **Completed** |
| #3 Danish Text                  | High     | 13d   | **Completed** |
| #4 Empty Project                | Low      | 5     | **Completed** |
| #5 DTO Validation               | Medium   | 3     | **Completed** |
| #6 Image Handling               | Medium   | 4     | **Completed** |
| #7 HttpRequest in Services      | High     | 13c   | **Completed** |
| #8 Hardcoded CORS               | Low      | 13    | Pending       |
| #9 Commented Code               | Low      | 6     | Pending       |
| #10 API Docs                    | Low      | 7     | Pending       |
| #11 MVC Exception Handling      | Low      | 10    | Pending       |
| #12 N+1 Queries                 | Medium   | 9     | Pending       |
| #13 JWT Key                     | Security | -     | Documented    |
| #14 Admin Password              | Security | -     | Documented    |
| #15 Test Coverage               | High     | 8     | Pending       |
| #17 Architecture Simplification | High     | 2.5   | **Completed** |
| #18 MVC Frontend Inconsistency  | High     | 2.6   | **Completed** |
| #19 Backend Architecture Review | High     | 13    | **Completed** |

---

## Phase 13: Backend Architecture Review (Feb 6, 2026)

Comprehensive review of Domain, Core, and API layers. All findings below are organized by severity and layer.

### HIGH PRIORITY — Correctness & Predictability

#### 19a. God DTO: `HouseDetailsDto` (21 params, 11 optional)

**Location**: `Sommerhus.Core/Dtos/Shared/HouseDetailsDto.cs`

**Problem**: A single record serves both Admin and Public contexts through optional parameters. Consumers must know which fields are populated in which context — this is unpredictable and error-prone. A colleague reading `HouseDetailsDto` has no way to know which fields will be `null` without reading both `AdminHouseService.MapDetails` and `HouseQueryService.GetAsync`.

**`HouseListItemDto` has the same problem** (14 params, many optional). `Images` is always an empty list in admin, while `Gallery` holds the actual images in public. Confusing overlap.

**Solution**: Split into `AdminHouseDetailsDto` and `PublicHouseDetailsDto`. Shared base fields can be a common interface or a nested record.

---

#### 19b. `PricePlanDetailsDto.planId` uses camelCase

**Location**: `Sommerhus.Core/Dtos/Shared/SeasonDto.cs:53`

**Problem**: Every other record parameter uses PascalCase. This one silently serializes as `planId` in JSON, breaking the naming convention and potentially confusing API consumers.

**Solution**: Rename to `PlanId`.

---

#### 19c. `UpsertHouseDto.Name` vs Entity `VacationHouse.Title`

**Location**: `Sommerhus.Core/Dtos/Admin/UpsertHouseDto.cs:10`

**Problem**: The DTO property is `Name`, the entity property is `Title`. Service maps `dto.Name → house.Title`. A developer looking at the DTO has no idea what "Name" refers to — it's the house title. Confusing and a source of bugs.

**Solution**: Rename DTO property to `Title` to match the entity.

---

#### 19d. `FeaturesController.UploadIcon` — Incomplete switch expression

**Location**: `Sommerhus.Api/Controllers/Admin/FeaturesController.cs:40-43`

**Problem**: The switch only handles `Success`. Any other status (NotFound, Invalid, etc.) will throw a `SwitchExpressionException` at runtime. This is a **bug**.

**Solution**: Use `this.FromResult(result)` like all other actions, or add missing cases.

---

#### 19e. `FeatureUpsertOutcome` breaks the `ServiceResult` pattern

**Location**: `Sommerhus.Core/Services/Admin/Houses/FeatureUpsertOutcome.cs`

**Problem**: Every other service returns `ServiceResult<T>`. This one uses a custom result type that doesn't integrate with `ControllerExtensions.FromResult()`. Forces manual mapping in the controller. A colleague encountering this will be confused about why this service is different.

**Solution**: Replace with `ServiceResult` or `ServiceResult<T>` to follow established pattern.

---

#### 19f. `Public.PricingController` depends on `IAdminPricingService`

**Location**: `Sommerhus.Api/Controllers/Public/PricingController.cs:10`

**Problem**: A public controller directly depends on an admin service interface. This breaks the admin/public separation that's used everywhere else in the codebase.

**Solution**: Extract `IPricingQuoteService` into `Core/Services/Public/Pricing/` with only the `QuoteAsync` method.

---

#### 19g. `DbContext.OnModelCreating` MaxLength conflicts with domain attributes

**Locations**:

- `VacationHouse.Title`: `[MaxLength(140)]` in domain vs `HasMaxLength(200)` in fluent config
- `HouseImage.FileName`: `[MaxLength(300)]` in domain vs `HasMaxLength(255)` in fluent config

**Problem**: Fluent API wins at runtime, so domain annotations are misleading. A developer reading the model class sees 140 chars max, but the DB allows 200. This creates subtle validation mismatches.

**Solution**: Pick one source of truth. Recommendation: use fluent API as authoritative and remove conflicting domain attributes, or match them exactly.

---

#### 19h. `SeasonPrice` double FK configuration

**Location**: `Sommerhus.Core/Data/DbContext.cs:151-154` and `162-165`

**Problem**: The FK from `SeasonPrice` to `PricePlan` is configured in both the `PricePlan` entity block and the `SeasonPrice` entity block. Only one should be authoritative; having both is confusing and could lead to unexpected cascade behavior changes.

**Solution**: Remove the duplicate; keep it in one place.

---

#### 19i. `ProblemDetailsMiddleware` leaks exception details in production

**Location**: `Sommerhus.Api/ProblemDetailsMiddleware.cs:33`

**Problem**: `Detail = ex.Message` is always set regardless of environment. In production, this could expose internal implementation details to attackers.

**Solution**: Only include `ex.Message` when `IHostEnvironment.IsDevelopment()` is true.

---

#### 19j. `PriceModifier.Value` uses SQLite-specific column type

**Location**: `Sommerhus.Core/Data/DbContext.cs:190`

**Problem**: `.HasColumnType("TEXT")` is SQLite-specific. On SQL Server, `decimal` should map to `decimal(18,2)`. This will cause issues when deploying to production with SQL Server.

**Solution**: Use conditional column type based on provider, or remove the explicit mapping and let EF choose the appropriate type per provider.

---

### MEDIUM PRIORITY — Consistency & Maintainability

#### 19k. `HttpRequest` passed to 8+ service interfaces

**Locations**: `IAdminHouseService`, `IAdminAreaService`, `IAdminFeatureService`, `IHouseQueryService`, all image services

**Problem**: Core layer depends on `Microsoft.AspNetCore.Http.HttpRequest`. This couples the business layer to ASP.NET Core, reducing testability and violating layer separation. Already documented as #7 but scope is larger than initially noted.

**Solution**: Pass a `string baseUrl` parameter instead, or inject an `IUrlBuilder` abstraction.

---

#### 19l. `CloneErrors` duplicated in 4 services

**Locations**: `AdminHouseService:284`, `AdminAreaService:282`, `AdminFeatureService:252`, `AdminHouseGroupService:256`

**Problem**: Identical ~7-line method copy-pasted across four services.

**Solution**: Move to `ServiceResult` as a static helper: `ServiceResult.CloneErrors(...)`, or an extension method in `Common/`.

---

#### 19m. Inconsistent private field naming convention

**Problem**: `AdminHouseGroupService` uses `_db` (underscore prefix) while `AdminHouseService`, `AdminAreaService`, `AdminFeatureService` use `db` (no prefix).

**Solution**: Pick one convention and apply it everywhere. The `.windsurfrules` says camelCase for private fields (no underscore).

---

#### 19n. `MapPlan` duplicated across two services

**Locations**: `AdminHouseService:295` and `AdminHousePricingService:80`

**Problem**: Identical mapping logic in two files. Changes to one must be mirrored in the other.

**Solution**: Extract to a shared static mapper class, e.g. `PricePlanMapper.ToDto(PricePlan)`.

---

#### 19o. `HouseQueryService.SearchAsync` returns `IEnumerable<T>` — no pagination metadata

**Location**: `Sommerhus.Core/Services/Public/Houses/IHouseQueryService.cs:9`

**Problem**: Admin search returns `PageResult<HouseListItemDto>` with total count, page info, etc. Public search returns raw `IEnumerable<HouseListItemDto>`. API consumer cannot implement pagination without knowing total count.

**Solution**: Return `PageResult<HouseListItemDto>` for consistency.

---

#### 19p. Duplicate `using` statements

**Locations**:

- `AdminHouseService.cs:10-11` — `Sommerhus.Core.Dtos.Admin` imported twice
- `IAdminHouseGroupService.cs:2,4` — `Sommerhus.Core.Dtos.Admin` imported twice
- `IAdminPricingService.cs:2,4` — same
- `Api/Extensions/ServiceCollectionExtensions.cs:1-17 vs 19-32` — entire block duplicated
- `CitiesController.cs:4-5` — `Sommerhus.Core.Dtos.Shared` imported twice

**Solution**: Remove all duplicate usings. Run `dotnet format`.

---

#### 19q. Inconsistent API route patterns

**Problem**:

- Admin controllers: `api/admin/{entity}` (explicit)
- `AuthController`: `admin/auth` (missing `api/` prefix)
- Public controllers: `api/[controller]` (convention-based) vs `api/pricing` (explicit)

**Solution**: Standardize all routes. Admin: `api/admin/{entity}`. Public: `api/{entity}`.

---

#### 19r. `AdminLoginRequest` / `AdminTokenResponse` are classes, not records

**Location**: `Sommerhus.Core/Dtos/Admin/AuthDtos.cs`

**Problem**: Convention says "prefer `record` for DTOs". These two are `sealed class`.

**Solution**: Convert to `sealed record`.

---

#### 19s. `PageResult<T>` is a class, not a record

**Location**: `Sommerhus.Core/Dtos/Shared/Paging.cs`

**Problem**: Same convention violation. Uses mutable `List<T>` property.

**Solution**: Convert to record with `IReadOnlyList<T>`.

---

#### 19t. `FeatureDto.ValueType` is stringly-typed

**Location**: `Sommerhus.Core/Dtos/Shared/FeatureDto.cs:12` and `UpsertFeatureDto:23`

**Problem**: Domain model has `FeatureValueType` enum but DTOs pass it as `string`. Service must parse it back. No compile-time safety.

**Solution**: Use the `FeatureValueType` enum directly in DTOs. JSON serialization handles enum↔string automatically.

---

### LOW PRIORITY — Cleanup & Hygiene

#### 19u. Orphaned domain models: `HouseAreas.cs` and `AreaCities.cs`

**Locations**: `Sommerhus.Domain/Models/HouseAreas.cs`, `AreaCities.cs`

**Problem**: Both M:N join tables are configured via `Dictionary<string, object>` in `OnModelCreating`. The explicit classes are never referenced by any service or DTO. `HouseAreas` also has `isPrimary` in camelCase (violating PascalCase convention) and uses block-scoped namespace.

**Solution**: Delete both files. They serve no purpose.

---

#### 19v. `VacationHouse.CoverImageId` — Dead property

**Location**: `Sommerhus.Domain/Models/VacationHouse.cs:18`

**Problem**: Nullable Guid that is never read or written by any service, DTO, or controller.

**Solution**: Remove the property and add a migration.

---

#### 19w. `VacationHouse.Facilities` — Nearly dead property

**Location**: `Sommerhus.Domain/Models/VacationHouse.cs:16`

**Problem**: Only used as a fallback in `HouseQueryService.BuildSummary()`. Not editable through any admin DTO or endpoint.

**Solution**: Either expose in `UpsertHouseDto` and admin UI, or remove it.

---

#### 19x. `SeasonCode.Name` defaults to Danish, `Color` has no default

**Location**: `Sommerhus.Domain/Models/Pricing/SeasonCode.cs:9-10`

**Problem**: `Name = "Højsæson"` is a Danish default in a domain model. `Color` is non-nullable string with no default — will cause NullReferenceException if not set.

**Solution**: Set `Name = ""` and `Color = "#6C757D"` as neutral defaults.

---

#### 19y. Inconsistent `Id` initialization across entities

**Problem**: Most entities have `Id = Guid.NewGuid()` but `HouseGroup.Id`, `SeasonSpan.Id` do not.

**Solution**: Add `= Guid.NewGuid()` to all entity `Id` properties for consistency.

---

#### 19z. Inconsistent image `FileName` MaxLength across entities

**Problem**: `HouseImage: 300`, `AreaImage: 200`, `CityImage: 260`. No reason for them to differ.

**Solution**: Standardize to 300 across all image entities.

---

#### 19aa. `DbSeeder` makes synchronous HTTP call and swallows all exceptions

**Location**: `Sommerhus.Core/Data/DbSeeder.cs:162-208`

**Problem**: `TryFetchDanishCities()` uses `.GetAwaiter().GetResult()` blocking the thread during startup, and catches ALL exceptions silently. If the external API is slow, startup blocks for 20 seconds.

**Solution**: Make the method async or move the HTTP fetch to a background task. Log exceptions instead of swallowing.

---

#### 19bb. `LookupItem.cs` has unnecessary using statements

**Location**: `Sommerhus.Core/Dtos/Shared/LookupItem.cs:1-5`

**Problem**: Four unused `using` directives.

**Solution**: Remove them. Run `dotnet format`.

---

#### 19cc. `Dtos/Public/` folder is empty

**Location**: `Sommerhus.Core/Dtos/Public/`

**Problem**: Empty directory adds noise to the project structure.

**Solution**: Delete the folder.

---

#### 19dd. Danish comment in CORS config

**Location**: `Sommerhus.Api/Program.cs:84`

**Problem**: `// eller dit domæne` — Danish comment in code.

**Solution**: Replace with English or remove.

---

#### 19ee. `HousesController.Create` inconsistent error handling

**Location**: `Sommerhus.Api/Controllers/Admin/HousesController.cs:31-41`

**Problem**: Manually maps `ServiceResult` via switch instead of using `this.FromResult()` like other actions. Also `UpsertPricing` (line 111) swallows actual error details with a generic message.

**Solution**: Use `this.FromResult()` consistently. Forward `result.Errors` in `UpsertPricing`.

---

#### 19ff. Public `HousesController` not `sealed`

**Location**: `Sommerhus.Api/Controllers/Public/HousesController.cs:9`

**Problem**: All admin controllers are `sealed class` but public ones are `class`. Inconsistent.

**Solution**: Make all controllers `sealed`.

---

#### 19gg. Stale documentation references

**Problem**: `ARCHITECTURE.md` dependency graph (line 350-374) still references `Sommerhus.Application`, `Sommerhus.Repository`, `Sommerhus.Contracts` — all deleted in Phase 2.5. `KNOWN_ISSUES.md` Phase 2.5/2.6 sections reference stale project names.

**Solution**: Update documentation to reflect current project structure.

---

## Phase 14: MVC Frontend Architecture Review (Feb 6, 2026)

Comprehensive review of the MVC presentation layer — Controllers, Services, ViewModels, Views, Extensions, and configuration. All findings organized by severity.

### HIGH PRIORITY — Predictability & Correctness

#### 20a. ✅ `LoginViewModel` defined inside `AccountController.cs`

**Location**: `Sommerhus.Mvc/Controllers/AccountController.cs:95-105`

**Problem**: A ViewModel class is defined in the same file as a Controller. This violates the separation of concerns and makes the ViewModel hard to discover. A colleague looking in `ViewModels/` will never find it. The Login view references it via `Sommerhus.Mvc.Controllers.LoginViewModel` — an unusual namespace for a ViewModel.

**Solution**: Move `LoginViewModel` to `Sommerhus.Mvc/ViewModels/Account/LoginViewModel.cs`.

---

#### 20b. ✅ Danish error messages in public controllers

**Locations**:

- `Sommerhus.Mvc/Controllers/Public/AreasController.cs:16` — `"Kunne ikke hente områderne."`
- `Sommerhus.Mvc/Controllers/Public/AreasController.cs:35` — `"Kunne ikke hente området."`
- `Sommerhus.Mvc/Controllers/Public/HousesController.cs:78` — `"Kunne ikke hente pris"`
- `Sommerhus.Mvc/Controllers/AccountController.cs:70` — `"Forkert brugernavn eller adgangskode."`

**Problem**: The `.windsurfrules` says "no Danish comments" and Phase 13d replaced all backend Danish. The MVC layer still has Danish error messages. Inconsistent language across the codebase.

**Solution**: Replace all Danish error messages with English equivalents.

---

#### 20c. ✅ Production `appsettings.Production.json` contains plaintext admin credentials

**Location**: `Sommerhus.Mvc/appsettings.Production.json:10-12`

**Problem**: `"Username": "admin", "Password": "sommerhus123"` is committed to source control in the production config. This is a **security issue**. The `AdminAuth` section appears unused by any code (no reference found), making it dead config that still leaks a credential.

**Solution**: Remove the `AdminAuth` section from production config. If needed, use environment variables.

---

#### 20d. ✅ `appsettings.Production.json` has malformed JSON structure

**Location**: `Sommerhus.Mvc/appsettings.Production.json:3`

**Problem**: The `Api.BaseUrl` value `"https://mikkel.smedt.dk/api/api"` has a double `/api/api` path which looks like a bug. Also, the JSON indentation is broken — the closing brace for `Api` is on the same line as `BaseUrl`, and subsequent keys are indented inside `Api` when they should be at root level.

**Solution**: Fix the URL and reformat the JSON properly.

---

#### 20e. ✅ `HouseGroupsController` and `PricesController` not `sealed`

**Locations**:

- `Sommerhus.Mvc/Controllers/Admin/HouseGroupsController.cs:9` — `public class`
- `Sommerhus.Mvc/Controllers/Admin/PricesController.cs:7` — `public class`

**Problem**: All other admin controllers (`HousesController`, `AreasController`, `FeaturesController`) are `sealed class`. These two are not. Inconsistent.

**Solution**: Add `sealed` keyword to both.

---

#### 20f. ✅ Inconsistent constructor patterns across controllers

**Locations**:

- `HousesController` — Traditional constructor with explicit field: `private readonly AdminApiClient _api;`
- `HouseGroupsController` — Primary constructor: `(AdminApiClient api)` accessing `api` directly
- `AreasController` — Traditional constructor with explicit field: `private readonly AdminApiClient _api;`
- `FeaturesController` — Traditional constructor with explicit field: `private readonly AdminApiClient _api;`
- `PricesController` — Traditional constructor with explicit field: `private readonly AdminApiClient _api;`
- Public `HousesController` — Primary constructor: `(SommerhusApi _api)` with underscore in parameter
- Public `AreasController` — Primary constructor: `(SommerhusApi api)` without underscore

**Problem**: Three different constructor patterns in the same project. A colleague cannot predict which pattern a new controller should use. The `.windsurfrules` says camelCase for private fields (no underscore prefix), but `_api` is used in 4 controllers.

**Solution**: Standardize on primary constructors (C# 12 feature, already available) with no underscore prefix. Example: `public sealed class HousesController(AdminApiClient api) : AdminControllerBase`.

---

#### 20g. ✅ Duplicate `using` statements

**Locations**:

- `Sommerhus.Mvc/Services/SommerhusApi.cs:1-2` — `using Sommerhus.Core.Dtos.Shared;` duplicated
- `Sommerhus.Mvc/Services/AdminApiClient.cs:3-4` — `using Sommerhus.Core.Dtos.Shared;` duplicated
- `Sommerhus.Mvc/Controllers/Public/HousesController.cs:2-3` — `using Sommerhus.Core.Dtos.Shared;` duplicated

**Solution**: Remove duplicate usings. Run `dotnet format`.

---

### MEDIUM PRIORITY — Consistency & Maintainability

#### 20h. ✅ `AdminApiClient` is a 200-line God class

**Location**: `Sommerhus.Mvc/Services/AdminApiClient.cs`

**Problem**: Single class handles API calls for Houses, Features, Areas, Pricing, House Groups, Season Spans, and Season Codes. This is the MVC equivalent of the backend's original `AdminHouseService` bloat. A colleague looking for "how do we call the areas API" must scan 200 lines.

**Solution**: Split into domain-specific clients: `AdminHouseApiClient`, `AdminAreaApiClient`, `AdminFeatureApiClient`, `AdminPricingApiClient`, `AdminHouseGroupApiClient`. Or at minimum, use `#region` blocks and XML doc comments to make sections discoverable.

---

#### 20i. ✅ Inconsistent ViewModel organization — mixed flat files and subfolders

**Location**: `Sommerhus.Mvc/ViewModels/Admin/`

**Problem**: Some ViewModels live in subfolders (`Houses/HouseListVm.cs`, `Areas/AreaListVm.cs`, `Features/FeatureListVm.cs`, `HouseGroups/HouseGroupDetailsVm.cs`), while others live as flat files in the parent (`HouseViewModels.cs`, `PricingViewModels.cs`). The `AreaViewModels.cs` file is a dead stub saying "Moved to Areas namespace". `HouseViewModels.cs` contains `HouseEditVm`, `HousePricingForm`, and `SeasonPriceRow` — three unrelated types in one file.

**Solution**:

1. Delete dead `AreaViewModels.cs`
2. Move `HouseEditVm` → `Houses/HouseEditVm.cs` (or delete if unused — `HouseCreateVm` and `HouseDetailsVm` exist)
3. Move `HousePricingForm` + `SeasonPriceRow` → `Houses/HousePricingForm.cs`
4. Move `PricingViewModels.cs` types → `Prices/PricingAdminVm.cs`
5. One ViewModel per file, matching the subfolder pattern

---

#### 20j. ✅ `HouseEditVm` appears unused

**Location**: `Sommerhus.Mvc/ViewModels/Admin/HouseViewModels.cs:8-13`

**Problem**: `HouseEditVm` has `Cities` and `Areas` as `IEnumerable<SelectListItem>`, but the actual edit flow uses `HouseDetailsVm` (which has `IReadOnlyList<SelectListItem>`). No controller or view references `HouseEditVm`. Dead code.

**Solution**: Verify it's unused and delete it.

---

#### 20k. ✅ Public controllers use `TempData["Err"]` directly; admin controllers use `SetError()`

**Locations**:

- `Sommerhus.Mvc/Controllers/Public/HousesController.cs:36` — `TempData["Err"] = ...`
- `Sommerhus.Mvc/Controllers/Public/AreasController.cs:16` — `TempData["Err"] = ...`
- `Sommerhus.Mvc/Controllers/Admin/*` — `SetError(...)` via `AdminControllerBase`

**Problem**: Two different patterns for the same thing. If someone changes the TempData key in `AdminControllerBase`, the public controllers break silently. Public controllers don't inherit from `AdminControllerBase` (correct — they shouldn't), but they should still use a consistent mechanism.

**Solution**: Extract `SetError`/`SetSuccess` into a shared base class (e.g., `SommerhusControllerBase`) or a static helper, so both admin and public controllers use the same TempData keys.

---

#### 20l. `ApiResponse<T>` is a class, not a record

**Location**: `Sommerhus.Mvc/Services/ApiResponse.cs`

**Problem**: Convention says "prefer `record` for DTOs and immutable data". `ApiResponse<T>` is effectively immutable (private constructor, `init` properties, static factory methods). Should be a record for consistency with the rest of the codebase.

**Solution**: Convert to `sealed record` or leave as-is with a comment explaining why (the static factory pattern is slightly awkward with records).

---

#### 20m. ✅ `SommerhusApi.GetCitiesAsync` calls admin endpoint from public client

**Location**: `Sommerhus.Mvc/Services/SommerhusApi.cs:31`

**Problem**: The public API client `SommerhusApi` calls `api/admin/cities/lookup` — an admin-only endpoint. This works because the public client doesn't attach auth tokens, and the endpoint may not require auth. But it's semantically wrong and will break if admin endpoints are locked down.

**Solution**: Either add a public cities endpoint (`api/cities/lookup`) or document why this is intentional.

---

#### 20n. ✅ Excessive blank lines throughout service and controller files

**Locations**: Multiple files have 3-8 consecutive blank lines (e.g., `AdminApiClient.cs:14-20`, `AdminApiClient.cs:60-65`, `HouseGroupsController.cs:140-152`)

**Problem**: Noise that makes files appear longer than they are. Inconsistent with the rest of the codebase.

**Solution**: Reduce to max 1 blank line between logical sections. Run `dotnet format`.

---

#### 20o. ✅ `.csproj` comments in Danish

**Location**: `Sommerhus.Mvc/Sommerhus.Mvc.csproj:4,7,12`

**Problem**: `"Opgradér til LTS"`, `"C# 12 følger med .NET 8"`, `"Ret stavefejl: Mcv -> Mvc"` — all Danish comments in the project file.

**Solution**: Replace with English or remove (they're historical notes, not needed).

---

#### 20p. Layout and views contain extensive Danish UI text with no localization

**Locations**:

- `Views/Shared/_Layout.cshtml` — Danish comments in CSS, Danish nav labels ("Forside", "Områder", "Log ud", "Log ind")
- `Views/Houses/Details.cshtml` — "Beskrivelse", "Beregn pris", "Ankomst", "Afrejse", "Gæster", "Nætter", "Moms", etc.
- `Views/Admin/HouseGroups/Details.cshtml` — "Rediger gruppe", "Tilbage til oversigt", "Sæsonkalender", "Tilføj sæsonperiode", etc.

**Problem**: This is a Danish-language product, so Danish UI text is expected. However, the codebase mixes Danish UI with English code comments, English error messages (admin), and Danish error messages (public). There's no localization framework — all strings are hardcoded.

**Solution**: This is a design decision, not a bug. If the product is Danish-only, document that convention. If multi-language support is planned, introduce `IStringLocalizer<T>` or resource files. At minimum, ensure error messages are consistently one language.

---

#### 20q. ✅ `ApiHttp.HandleResponseAsync` reads response body multiple times on error

**Location**: `Sommerhus.Mvc/Services/ApiHttp.cs:62-117`

**Problem**: On 400/422 responses, the code tries to deserialize the body as `ValidationProblemDetails`, then `Dictionary<string, string[]>`, then `ProblemDetails`, then raw text — each in a separate try/catch. After the first `ReadFromJsonAsync`, the stream is consumed. Subsequent reads may fail or return empty data depending on buffering. The empty `catch` blocks swallow all exceptions silently.

**Solution**: Read the response body once as a string, then attempt to deserialize from that string. Log or at least comment the catch blocks.

---

#### 20r. ✅ `AdminApiClient.GetHouseGroupsAsync` returns `IReadOnlyList<LookupItem>` but `GetHouseGroupListAsync` returns `IReadOnlyList<HouseGroupDto>` — same endpoint

**Location**: `Sommerhus.Mvc/Services/AdminApiClient.cs:139-143`

**Problem**: Both methods call the exact same URL (`api/admin/house-groups`) but deserialize into different types. This is confusing — a colleague doesn't know which to use. The `LookupItem` version loses data.

**Solution**: Remove `GetHouseGroupsAsync` (the `LookupItem` version) and have callers map `HouseGroupDto → LookupItem` if needed. Or rename to make the distinction clear.

---

### LOW PRIORITY — Cleanup & Hygiene

#### 20s. ✅ Commented-out code in public `HousesController`

**Location**: `Sommerhus.Mvc/Controllers/Public/HousesController.cs:81-87`

**Problem**: Dead commented-out `DeleteImage` action. Clutters the file.

**Solution**: Delete it.

---

#### 20t. ✅ `_ViewImports.cshtml` imports `Sommerhus.Mvc.Controllers` globally

**Location**: `Sommerhus.Mvc/Views/_ViewImports.cshtml:2`

**Problem**: This import exists solely so `Login.cshtml` can reference `LoginViewModel` from the Controllers namespace. Once `LoginViewModel` is moved to ViewModels (20a), this import becomes unnecessary and pollutes all views with controller types.

**Solution**: Remove after fixing 20a.

---

#### 20u. ✅ Inline CSS in `_Layout.cshtml` instead of external stylesheet

**Location**: `Sommerhus.Mvc/Views/Shared/_Layout.cshtml:17-63`

**Problem**: ~45 lines of CSS embedded in the layout. Changes require editing the layout file. Not cacheable separately. Mixes concerns.

**Solution**: Move to `wwwroot/css/site-additions.css` (which is already referenced on line 13 but apparently doesn't contain these styles).

---

#### 20v. `AdminAuth` config section appears unused

**Locations**: `appsettings.Development.json:8-11`, `appsettings.Production.json:10-12`

**Problem**: `AdminAuth.Username` and `AdminAuth.Password` are defined but never read by any code. The MVC app authenticates via the API's login endpoint, not via local config. Dead configuration that leaks credentials.

**Solution**: Remove from all appsettings files.

---

#### 20w. ✅ No global exception handling middleware in MVC

**Location**: `Sommerhus.Mvc/Program.cs`

**Problem**: No `app.UseExceptionHandler()` or custom middleware. Unhandled exceptions will show the default developer exception page in development and a blank 500 in production. Already documented as #11 but worth re-emphasizing.

**Solution**: Add `app.UseExceptionHandler("/error")` with a friendly error page.

---

#### 20x. ✅ `_Layout.cshtml` has htmx CDN fallback but no local htmx file check

**Location**: `Sommerhus.Mvc/Views/Shared/_Layout.cshtml:97-104`

**Problem**: References `~/lib/htmx/htmx.min.js` locally, then falls back to unpkg CDN. If the local file doesn't exist, every page load makes an external request. No SRI hash on the CDN fallback.

**Solution**: Ensure local htmx file exists in `wwwroot/lib/htmx/`, or add SRI hash to CDN fallback.

#### 20y. ✅ Public `Houses` view uses `ViewBag` instead of a typed ViewModel

**Location**: `Sommerhus.Mvc/Controllers/Public/HousesController.cs:29-31`, `Views/Houses/Houses.cshtml:5-7`, `Views/Home/Index.cshtml:5-7`

**Problem**: The `Houses` action stuffs `Query`, `Area`, and `Areas` into `ViewBag`, which is untyped and fragile. Views cast from `ViewBag` with `(string)(ViewBag.Query ?? "")` — any rename silently breaks at runtime.

**Solution**: Create `ViewModels/Public/Houses/HouseListVm` with typed properties and pass it as the model.

---

#### 20z. ✅ Public views read `TempData["Err"]` directly instead of shared partial

**Location**: `Views/Areas/Index.cshtml:6`, `Views/Areas/Details.cshtml:5`

**Problem**: Public views manually read `TempData["Err"]` and render their own alert markup. Admin views use `_FlashMessages.cshtml`. Two patterns for the same thing.

**Solution**: Create `Views/Shared/_FlashMessages.cshtml` (reads both `TempData["Err"]` and `TempData["Ok"]`) and use it in all public views.

---

#### 20aa. ✅ Mojibake encoding in `Areas/Details.cshtml`

**Location**: `Sommerhus.Mvc/Views/Areas/Details.cshtml:12,28,64`

**Problem**: Danish characters rendered as `p� omr�det`, `omr�de`, `omr�de` — file was saved with wrong encoding.

**Solution**: Re-save with UTF-8 encoding and correct characters.

---

#### 20ab. ✅ Unnecessary `@using` directives in `_HouseCard.cshtml`

**Location**: `Sommerhus.Mvc/Views/Houses/_HouseCard.cshtml:1-3`

**Problem**: `@using System`, `@using System.Collections.Generic`, `@using System.Linq` are redundant — these are globally available via `GlobalUsings.cs`.

**Solution**: Remove the unnecessary usings.

---

## Phase 15: Post-Refactor Bug Fixes & Consistency Audit (Feb 8, 2026)

### 21. MVC AreasController Bugs ✅ FIXED

**Location**: `Sommerhus.Mvc/Controllers/Admin/AreasController.cs`

**Problems found**:

- **21a** `Create` action rendered wrong view (`~/Views/Admin/Houses/Create.cshtml` instead of `~/Views/Admin/Areas/Details.cshtml`) — would crash at runtime
- **21b** `Update` action cast `IReadOnlyList<Guid>` to `List<Guid>` — `InvalidCastException` at runtime
- **21c** `BuildAreaEditVmAsync` could assign `null` to `required AreaDetailsDto Area` property — crash at runtime
- **21d** `Update` checked `res.Data is null` on `ApiResponse<object?>` — unnecessary and misleading

### 22. Compiler Warnings (8 total) ✅ FIXED

- **22a** Duplicate `using Sommerhus.Core.Dtos.Shared` in `Api/Controllers/Admin/CitiesController.cs` (CS0105)
- **22b** Nullable dereference `res.Data.Id` in MVC `AreasController.Create` (CS8602)
- **22c** Nullability mismatch in `AdminApiClient.GetHousesAsync` return type (CS8619)
- **22d** `async` method without `await` in MVC `HousesController.AdminIndex` (CS1998)
- **22e** Null source to `OrderBy` in `HouseGroups/Details.cshtml` Razor view (CS8604)

### 23. Danish Text Remaining ✅ FIXED

- **23a** 4 Danish error messages in `AdminFeatureService.cs` (`Ugyldig`, `Ingen fil`, `Fil er for stor`, `Kun PNG`)
- **23b** 2 Danish comments in `StorageOptions.cs` (`valgfrit`, `hvis SQLite`)

### 24. Spurious Using ✅ FIXED

- **24a** `UpsertAreaDto.cs` imported `Microsoft.Identity.Client` (wrong package, unused)

---

### Phase 15b: Codebase Consistency ✅ COMPLETED (Feb 8, 2026)

The following consistency improvements were identified and fixed during the audit.

### 25. DTO Style Inconsistency (Pending)

**Problem**: Some DTOs use `record` (immutable), others use `record` with mutable `{ get; set; }`. Some write DTOs are `class`, others are `record`.

| DTO                   | Style                                 | Should Be                                               |
| --------------------- | ------------------------------------- | ------------------------------------------------------- |
| `UpsertAreaDto`       | `record` with `{ get; set; }`         | `class` (mutable, form-bound)                           |
| `UpsertHouseDto`      | `sealed class`                        | OK                                                      |
| `UpsertHouseGroupDto` | `sealed record` with `{ get; init; }` | `sealed class` (for consistency with other Upsert DTOs) |
| `UpsertSeasonSpanDto` | `sealed record` with `{ get; init; }` | `sealed class` (for consistency)                        |

**Solution**: Standardize all Upsert DTOs to `sealed class` with `{ get; set; }` for MVC form binding consistency.

### 26. MVC Controller Pattern Inconsistency ✅ FIXED

**Problem**: Area and House MVC controllers followed different patterns:

- **AreasController**: `New` rendered `Details.cshtml` with `AreaCreateVm` (type mismatch), `_TabOverview.cshtml` was a duplicate of `Create.cshtml`
- **HousesController**: Separate `Create.cshtml`/`Details.cshtml` with matching ViewModels

**Fix**: Standardized Areas to match Houses pattern:

- `New` → renders `Create.cshtml` with `AreaCreateVm`
- `Details` → renders `Details.cshtml` with `AreaDetailsVm` (now loads cities for edit form)
- `_TabOverview.cshtml` rewritten as clean edit-only partial (matching Houses `_TabOverview.cshtml`)
- Removed duplicate content from `Create.cshtml`

### 27. AreaCreateVm Namespace Mismatch ✅ FIXED

**Fix**: Changed namespace from `Sommerhus.Mvc.ViewModels.Admin.Area` to `Sommerhus.Mvc.ViewModels.Admin.Areas`. Removed unused `Houses` namespace import from `AreasController`.

### 28. Image Service Interface Inconsistency (Pending)

**Problem**: Image services have dedicated interfaces but don't implement the generic `IAdminEntityImageService<T>` interfaces defined in `IAdminEntityImageService.cs`.

**Solution**: Either implement the generic interfaces or remove the unused generic interface definitions.

### 29. Admin API Controller Pattern Inconsistency ✅ FIXED

**Problems fixed**:

- `CitiesController.GetAll/Lookup`: Removed unnecessary `async/await` (now direct `Task` return)
- `FeaturesController.GetAll`: Removed unnecessary `async/await`
- `HouseGroupsController.Lookup`: Removed unnecessary `async/await`
- `AreasController.Create/Update/Delete`: Replaced manual `ServiceResultStatus` switch with `FromResult` helper
- `CityImagesController.Upload`: Replaced manual `ServiceResultStatus` check with `FromResult`, standardized `RequestSizeLimit` to 25MB
- Removed unused `using Sommerhus.Core.Common` from `AreaImagesController` and `CityImagesController`
- Removed unused `using Microsoft.AspNetCore.Http` from `AreasController`

### 30. Inconsistent Constructor Styles in Core Services (Pending)

**Problem**: Some services use primary constructors, others use traditional constructors with field assignment.

**Solution**: Convert all Core services to primary constructors (C# 12 feature, already used in controllers).

### 31. House Image Kind Bug ✅ FIXED

**Location**: `Sommerhus.Mvc/Views/Admin/Houses/_TabImages.cshtml`

**Problem**: The image kind selector only had a single "Hero" button sending `kind=hero`, but the `ImageKind` enum defines `Cover`, `Gallery`, `Floorplan`. The "hero" value would fail silently or be rejected by the API.

**Fix**: Replaced single "Hero" button with three buttons (Cover/Gallery/Floorplan) matching the `ImageKind` enum. Active kind is visually highlighted.

### 32. Area Image Upload — Single File Only ✅ FIXED

**Problem**: Area images could only be uploaded one at a time, while houses supported batch upload.

**Fix**: Full stack update to support batch upload:

- `IAdminAreaImageService`: Added `UploadAsync(Guid, IFormFileCollection, ...)` overload
- `AdminAreaImageService`: Implemented batch upload (matching `AdminHouseImageService` pattern)
- `AreaImagesController`: Changed from `IFormFile file` to `[FromForm] IFormFileCollection files`
- `AdminApiClient`: Replaced `UploadAreaImageAsync` (single) with `UploadAreaImagesAsync` (batch)
- MVC `AreasController`: New `UploadAreaImages` action with count feedback
- `_TabImages.cshtml`: Multi-file input with card grid layout matching houses

### 33. Area Image Delete Route Inconsistency ✅ FIXED

**Problem**: Area image delete used `/admin/areas/{id}/images/delete` with `imageId` as form field. Houses used `/admin/areas/{id}/images/{imageId}/delete` with `imageId` in route.

**Fix**: Standardized to `/admin/areas/{id}/images/{imageId}/delete` matching houses. Renamed action to `DeleteAreaImage` (matching `DeleteHouseImage`). Removed unused `RedirectAfterImageChange` helper.

### 34. Areas Details Action Missing Cities ✅ FIXED

**Problem**: `AreasController.Details` fetched gallery images but never used them, and didn't load cities for the overview tab's edit form — the city dropdown would be empty.

**Fix**: Removed unused image fetch logic, added `LoadCityOptionsAsync` call, and passed cities to `AreaDetailsVm`.

---

### Tracking Update

| Issue                               | Priority | Phase | Status  |
| ----------------------------------- | -------- | ----- | ------- |
| #20a LoginViewModel in controller   | High     | 14a   | ✅ Done |
| #20b Danish error messages in MVC   | High     | 14a   | ✅ Done |
| #20c Plaintext credentials in prod  | High     | 14a   | ✅ Done |
| #20d Malformed production JSON      | High     | 14a   | ✅ Done |
| #20e Controllers not sealed         | Medium   | 14a   | ✅ Done |
| #20f Inconsistent constructor style | High     | 14a   | ✅ Done |
| #20g Duplicate usings               | Low      | 14a   | ✅ Done |
| #20h AdminApiClient God class       | Medium   | 14c   | ✅ Done |
| #20i Mixed ViewModel organization   | Medium   | 14b   | ✅ Done |
| #20j Dead HouseEditVm               | Low      | 14b   | ✅ Done |
| #20k Inconsistent TempData pattern  | Medium   | 14b   | ✅ Done |
| #20l ApiResponse not a record       | Low      | 14    | Pending |
| #20m Public client calls admin API  | Medium   | 14c   | ✅ Done |
| #20n Excessive blank lines          | Low      | 14c   | ✅ Done |
| #20o Danish csproj comments         | Low      | 14d   | ✅ Done |
| #20p Mixed language in UI           | Low      | 14    | Pending |
| #20q Response body read multiple x  | Medium   | 14c   | ✅ Done |
| #20r Duplicate API client methods   | Medium   | 14c   | ✅ Done |
| #20s Commented-out code             | Low      | 14a   | ✅ Done |
| #20t Unnecessary ViewImport         | Low      | 14b   | ✅ Done |
| #20u Inline CSS in layout           | Low      | 14d   | ✅ Done |
| #20v Unused AdminAuth config        | Medium   | 14a   | ✅ Done |
| #20w No exception handling          | Medium   | 14d   | ✅ Done |
| #20x htmx CDN without SRI           | Low      | 14d   | ✅ Done |
| #20y ViewBag in public Houses view  | High     | 14c   | ✅ Done |
| #20z Direct TempData in pub views   | Medium   | 14c   | ✅ Done |
| #20aa Mojibake in Areas/Details     | Medium   | 14c   | ✅ Done |
| #20ab Redundant usings in HouseCard | Low      | 14c   | ✅ Done |
| #25 DTO style inconsistency         | Low      | 15b   | Pending |
| #26 MVC controller pattern          | High     | 15b   | ✅ Done |
| #27 AreaCreateVm namespace          | Medium   | 15b   | ✅ Done |
| #28 Image service interfaces        | Low      | 15b   | Pending |
| #29 API controller patterns         | Medium   | 15b   | ✅ Done |
| #30 Core service constructors       | Low      | 15b   | Pending |
| #31 House image kind bug            | High     | 15b   | ✅ Done |
| #32 Area image batch upload         | High     | 15b   | ✅ Done |
| #33 Area image delete route         | Medium   | 15b   | ✅ Done |
| #34 Areas Details missing cities    | High     | 15b   | ✅ Done |

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

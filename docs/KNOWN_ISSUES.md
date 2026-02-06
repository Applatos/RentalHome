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
| #3 Danish Text                  | High     | 13    | Pending       |
| #4 Empty Project                | Low      | 5     | **Completed** |
| #5 DTO Validation               | Medium   | 3     | **Completed** |
| #6 Image Handling               | Medium   | 4     | **Completed** |
| #7 HttpRequest in Services      | High     | 13    | Pending       |
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
| #19 Backend Architecture Review | High     | 13    | **13a Done**  |

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

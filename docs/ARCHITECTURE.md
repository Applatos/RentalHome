# Sommerhus Architecture Documentation

## Overview

Sommerhus is a vacation house rental platform built with ASP.NET Core 8. The solution uses a **simplified layered architecture** that balances separation of concerns with practical maintainability.

---

## Solution Structure

```
Sommerhus_project/
├── Sommerhus.Api/           # REST API (presentation layer)
├── Sommerhus.Mvc/           # Razor MVC frontend
│   ├── ViewModels/          # Extracted view models
│   └── Extensions/          # Helper extensions (e.g., SelectListExtensions)
├── Sommerhus.Domain/        # Entity models (pure POCOs)
├── Sommerhus.Core/          # Business logic + Data access (merged layer)
│   ├── Dtos/                # Admin, Public, Shared DTOs
│   ├── Services/            # Service interfaces + implementations
│   ├── Data/                # EF Core DbContext
│   ├── Identity/            # ASP.NET Identity
│   └── Common/              # ServiceResult, utilities
└── Sommerhus.Api.Tests/     # Integration tests
```

---

## Layer Responsibilities

### 1. Sommerhus.Domain (Core Layer)

**Purpose**: Pure domain entities with no external dependencies.

```
Models/
├── VacationHouse.cs      # Main entity - vacation rental property
├── City.cs               # Location entity
├── Area.cs               # Geographic region grouping cities
├── Feature.cs            # Property features (pool, wifi, etc.)
├── HouseFeatureValue.cs  # Feature values per house
├── HouseImage.cs         # Image metadata
├── HouseGroup.cs         # Grouping for pricing
└── Pricing/              # Pricing-related entities
    ├── PricePlan.cs
    ├── SeasonPrice.cs
    ├── SeasonCode.cs
    └── SeasonSpan.cs
```

**Rules**:

- No NuGet dependencies (except System.ComponentModel.Annotations)
- No references to other projects
- Only POCO classes with validation attributes

---

### 2. Sommerhus.Core (Business + Data Layer)

**Purpose**: Combined business logic, service interfaces/implementations, DTOs, and data access.

```
Core/
├── Dtos/
│   ├── Shared/          # Consolidated DTOs (1-2 per entity)
│   │   ├── AreaDetailsDto.cs      # Area read DTO with admin fields
│   │   ├── AreaListItemDto.cs     # Area list DTO
│   │   ├── CityDto.cs             # CityDto + UpsertCityDto (consolidated)
│   │   ├── FeatureDto.cs          # FeatureDto + UpsertFeatureDto (consolidated)
│   │   ├── FeatureValueDto.cs     # Feature value for house
│   │   ├── HouseDetailsDto.cs     # Admin + Public house detail DTOs
│   │   ├── HouseListItemDto.cs    # Admin + Public house list DTOs
│   │   ├── HouseGroupDto.cs       # HouseGroupDto + UpsertHouseGroupDto (consolidated)
│   │   ├── SeasonDto.cs           # All pricing DTOs consolidated
│   │   ├── PriceQuoteDto.cs       # Pricing request/response DTOs
│   │   ├── Images.cs              # Image DTOs
│   │   ├── LookupItem.cs          # Simple lookup DTO
│   │   ├── Paging.cs              # Pagination DTOs
│   │   └── PostFeatureValueDto.cs # Feature value creation DTO
│   ├── Admin/           # Entity-specific write DTOs
│   │   ├── UpsertAreaDto.cs       # Area create/update DTO
│   │   ├── UpsertHouseDto.cs      # House create/update DTO
│   │   └── AuthDtos.cs            # Authentication DTOs
│   └── Security/
│       └── AdminRoles.cs          # Role constants
├── Services/
│   ├── Admin/                # Admin service interfaces + implementations
│   │   ├── Houses/
│   │   │   ├── IAdminHouseService.cs
│   │   │   └── AdminHouseService.cs
│   │   ├── Areas/
│   │   ├── Cities/
│   │   └── Features/
│   ├── Public/               # Public service interfaces + implementations
│   │   └── Houses/
│   │       ├── IHouseQueryService.cs
│   │       └── HouseQueryService.cs
│   ├── Pricing/              # Pricing engine
│   │   ├── Abstractions/
│   │   └── Engine/
│   └── Storage/
│       └── IImageStorage.cs
├── Data/
│   ├── DbContext.cs          # EF Core context
│   └── DbSeeder.cs           # Development seeding
├── Identity/
│   ├── ApplicationUser.cs
│   └── AdminIdentitySeeder.cs
├── Common/
│   └── ServiceResult.cs      # Standard result wrapper
└── ServiceCollectionExtensions.cs
```

**Key Patterns**:

- **ServiceResult<T>**: Wraps operation outcomes with success/failure states
- **Colocation**: Interfaces and implementations live together for easier navigation
- **Admin vs Public**: Separate services for different access patterns

**Database Support**:

- SQLite (development)
- SQL Server (production)
- Provider auto-detection from connection string

---

### 3. Sommerhus.Api (Presentation Layer)

**Purpose**: REST API endpoints with JWT authentication.

```
Api/
├── Controllers/
│   ├── Admin/            # Protected endpoints (require Admin role)
│   │   ├── HousesController.cs
│   │   ├── AreasController.cs
│   │   └── AuthController.cs
│   └── Public/           # Open endpoints
│       ├── HousesController.cs
│       └── PricingController.cs
├── Infrastructure/
│   ├── Auth/
│   │   └── JwtOptions.cs
│   ├── Storage/
│   │   └── PhysicalImageStorage.cs
│   └── ControllerExtensions.cs
└── Program.cs            # Application entry point
```

**Authentication**:

- JWT Bearer tokens for API authentication
- Admin endpoints require `Admin` role claim

---

### 4. Sommerhus.Mvc (Frontend)

**Purpose**: Server-rendered Razor views consuming the API.

```
Mvc/
├── Controllers/
│   ├── Admin/                    # Admin dashboard
│   │   ├── AdminControllerBase.cs    # Shared helpers (SetSuccess, SetError, etc.)
│   │   ├── HousesController.cs       # House CRUD (Index, Details, Create, Edit, Delete)
│   │   ├── HousePricingController.cs # House pricing + calendar
│   │   ├── AreasController.cs        # Area CRUD
│   │   ├── FeaturesController.cs     # Feature CRUD
│   │   ├── HouseGroupsController.cs  # House group CRUD
│   │   └── PricingController.cs      # Season codes management
│   ├── Public/                   # Public pages
│   │   ├── HousesController.cs
│   │   └── AreasController.cs
│   └── AccountController.cs
├── ViewModels/
│   └── Admin/                    # Typed ViewModels (never raw DTOs in views)
│       ├── Houses/
│       │   ├── HouseListVm.cs
│       │   ├── HouseDetailsVm.cs
│       │   └── HouseCreateVm.cs
│       ├── Areas/
│       │   └── AreaViewModels.cs
│       └── PricingViewModels.cs
├── Extensions/
│   └── SelectListExtensions.cs
├── Views/
│   ├── Admin/
│   │   ├── Houses/               # House views (folder per entity)
│   │   │   ├── Index.cshtml
│   │   │   ├── Details.cshtml
│   │   │   └── _Tab*.cshtml
│   │   ├── Areas/
│   │   ├── Features/
│   │   ├── HouseGroups/
│   │   └── Shared/
│   │       ├── _FlashMessages.cshtml
│   │       └── _Pagination.cshtml
│   ├── Houses/                   # Public house views
│   └── Shared/                   # Layouts
├── Services/
│   ├── AdminApiClient.cs         # Typed HTTP client for admin API
│   ├── SommerhusApi.cs           # Public API client
│   └── AdminApiAuthHandler.cs
└── Program.cs
```

**MVC Admin Conventions**:

| Convention        | Pattern                                        |
| ----------------- | ---------------------------------------------- |
| Controller naming | `{Entity}Controller`                           |
| Action names      | `Index`, `Details`, `Create`, `Edit`, `Delete` |
| Route pattern     | `/admin/{entity}`, `/admin/{entity}/{id}`      |
| ViewModels        | Always typed, never raw DTOs                   |
| ViewBag           | Never used (all data in ViewModel)             |
| Flash messages    | `SetSuccess()`, `SetError()` helpers           |
| View folders      | `Views/Admin/{Entity}/`                        |
| Partials          | `_Tab{Name}.cshtml`, `_FlashMessages.cshtml`   |

**Authentication**:

- Cookie-based for web sessions
- Stores JWT token and forwards to API calls

---

## Data Flow

### Admin Flow (Create House)

```
Browser → MVC Controller → AdminApiClient → API Controller → AdminHouseService → DbContext → Database
```

### Public Flow (View House)

```
Browser → MVC Controller → SommerhusApi → API Controller → HouseQueryService → DbContext → Database
```

---

## Key Design Patterns

### 1. ServiceResult Pattern

All service methods return `ServiceResult<T>` for consistent error handling:

```csharp
public async Task<ServiceResult<HouseDetailsDto>> GetDetailsAsync(Guid id, ...)
{
    var house = await db.Houses.FindAsync(id);
    if (house is null)
        return ServiceResult<HouseDetailsDto>.NotFound();

    return ServiceResult<HouseDetailsDto>.Success(MapToDto(house));
}
```

### 2. Thin Controllers

Controllers only orchestrate; business logic lives in services:

```csharp
[HttpGet("{id:guid}")]
public async Task<ActionResult<HouseDetailsDto>> Get(Guid id, CancellationToken ct)
    => this.FromResult(await service.GetDetailsAsync(id, Request.BaseUrl(), ct));
```

### 3. Image Storage Abstraction

`IImageStorage` abstracts file operations for testability:

```csharp
public interface IImageStorage
{
    Task<StoredImage> SaveAsync(ImageCategory category, Guid ownerId, IFormFile file, CancellationToken ct);
    Task DeleteAsync(ImageCategory category, Guid ownerId, string fileName, CancellationToken ct);
    string? GetUrl(string baseUrl, ImageCategory category, Guid ownerId, string fileName);
}
```

---

## Database Schema (Key Entities)

```
VacationHouse
├── Id (PK)
├── Title
├── Address
├── CityId (FK → City)
├── Description
├── GroupId (FK → HouseGroup, nullable)
└── CreatedUtc

City
├── Id (PK)
├── Name
├── Zip
└── Areas (M:N via AreaCities)

Area
├── Id (PK)
├── Name
├── Cities (M:N via AreaCities)
└── Houses (M:N via HouseAreas)

PricePlan
├── Id (PK)
├── HouseId (FK)
├── Name
├── Currency
├── IsActive
└── SeasonPrices (1:N)
```

---

## Configuration

### Environment-Based Config

| File                           | Purpose             |
| ------------------------------ | ------------------- |
| `appsettings.json`             | Base configuration  |
| `appsettings.Development.json` | Local dev overrides |
| `appsettings.Testing.json`     | Test environment    |
| `appsettings.Production.json`  | Production settings |

### Key Configuration Sections

```json
{
  "DatabaseProvider": "Sqlite|SqlServer",
  "ConnectionStrings": { "Default": "..." },
  "Jwt": { "Issuer", "Audience", "Key", "AccessTokenMinutes" },
  "DefaultAdmin": { "UserName", "Password", "Email" },
  "Pricing": { "EnabledV1", "CleaningFee", "VatRate" }
}
```

---

## Dependency Graph

```
Sommerhus.Api
    ├── Sommerhus.Core
    └── Sommerhus.Domain (transitive via Core)

Sommerhus.Mvc
    ├── Sommerhus.Core (for DTOs and service interfaces)
    └── Sommerhus.Domain (transitive via Core)

Sommerhus.Core
    └── Sommerhus.Domain

Sommerhus.Domain
    └── (none)

Sommerhus.Api.Tests
    └── Sommerhus.Api (via WebApplicationFactory)
```

---

## Testing Strategy

### Integration Tests (Sommerhus.Api.Tests)

- Uses `WebApplicationFactory` for full request/response testing
- In-memory SQLite for isolated database
- Auto-seeds admin user for authenticated tests

### Test Organization

```
Api.Tests/
├── Admin/           # Admin endpoint tests
├── Public/          # Public endpoint tests
├── Application/     # Business logic unit tests
└── Infrastructure/  # Test fixtures
```

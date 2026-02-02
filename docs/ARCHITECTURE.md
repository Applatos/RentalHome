# Sommerhus Architecture Documentation

## Overview

Sommerhus is a vacation house rental platform built with ASP.NET Core 8 following **Clean Architecture** principles. The solution separates concerns across multiple projects to maintain testability, flexibility, and maintainability.

---

## Solution Structure

```
Sommerhus_project/
├── Sommerhus.Api/           # REST API (presentation layer)
├── Sommerhus.Mvc/           # Razor MVC frontend
├── Sommerhus.Domain/        # Entity models (core)
├── Sommerhus.Application/   # Business logic interfaces & contracts
├── Sommerhus.Repository/    # Data access implementations
├── Sommerhus.Contracts/     # Shared DTOs
├── Sommerhus.Api.Tests/     # Integration tests
└── Sommerhus.Pricing/       # (Empty - reserved for pricing engine)
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

### 2. Sommerhus.Application (Business Layer)
**Purpose**: Service interfaces, DTOs, and business logic contracts.

```
Application/
├── Admin/                    # Admin-only operations
│   ├── Houses/
│   │   └── IAdminHouseService.cs
│   ├── Areas/
│   ├── Cities/
│   ├── Features/
│   └── Images/
├── Public/                   # Public-facing queries
│   ├── Houses/
│   │   └── IHouseQueryService.cs
│   └── ...
├── Common/
│   └── ServiceResult.cs      # Standard result wrapper
├── Pricing/
│   ├── Abstractions/
│   │   └── IPricingPipeline.cs
│   └── Engine/               # Pricing calculation rules
└── Storage/
    └── IImageStorage.cs      # File storage abstraction
```

**Key Patterns**:
- **ServiceResult<T>**: Wraps operation outcomes with success/failure states
- **Separation**: Admin vs Public services for different access patterns

---

### 3. Sommerhus.Repository (Infrastructure Layer)
**Purpose**: EF Core implementations and data access.

```
Repository/
├── Data/
│   ├── DbContext.cs          # EF Core context with Fluent API config
│   └── DbSeeder.cs           # Development data seeding
├── Identity/
│   ├── ApplicationUser.cs    # ASP.NET Identity user
│   └── AdminIdentitySeeder.cs
├── Admin/
│   └── Houses/
│       └── AdminHouseService.cs  # Implements IAdminHouseService
├── Public/
│   └── Houses/
│       └── HouseQueryService.cs
├── Pricing/
│   └── EfRatePlanStore.cs
└── ServiceCollectionExtensions.cs  # DI registration
```

**Database Support**:
- SQLite (development)
- SQL Server (production)
- Provider auto-detection from connection string

---

### 4. Sommerhus.Contracts (Shared DTOs)
**Purpose**: Data transfer objects shared between API and MVC.

```
Contracts/
├── Dtos/
│   ├── Admin/            # Admin-specific DTOs
│   │   ├── Houses/
│   │   ├── Areas/
│   │   └── Features/
│   ├── Public/           # Public-facing DTOs
│   └── Shared/           # Common DTOs
│       ├── ImageDto.cs
│       ├── LookupItem.cs
│       └── PageResult.cs
└── Security/
    └── AdminRoles.cs     # Role constants
```

---

### 5. Sommerhus.Api (Presentation Layer)
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

### 6. Sommerhus.Mvc (Frontend)
**Purpose**: Server-rendered Razor views consuming the API.

```
Mvc/
├── Controllers/
│   ├── Admin/            # Admin dashboard
│   │   └── AdminController.cs
│   ├── Public/           # Public pages
│   │   └── HousesController.cs
│   └── AccountController.cs
├── Views/
│   ├── Admin/            # Admin views
│   ├── Houses/           # House listing/details
│   └── Shared/           # Layouts, partials
├── Services/
│   ├── AdminApiClient.cs     # Typed HTTP client for admin API
│   ├── SommerhusApi.cs       # Public API client
│   └── AdminApiAuthHandler.cs
└── Program.cs
```

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
    => this.FromResult(await service.GetDetailsAsync(id, Request, ct));
```

### 3. Image Storage Abstraction
`IImageStorage` abstracts file operations for testability:
```csharp
public interface IImageStorage
{
    Task<StoredImage> SaveAsync(ImageCategory category, Guid ownerId, IFormFile file, CancellationToken ct);
    Task DeleteAsync(ImageCategory category, Guid ownerId, string fileName, CancellationToken ct);
    string GetUrl(HttpRequest request, ImageCategory category, Guid ownerId, string fileName);
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
| File | Purpose |
|------|---------|
| `appsettings.json` | Base configuration |
| `appsettings.Development.json` | Local dev overrides |
| `appsettings.Testing.json` | Test environment |
| `appsettings.Production.json` | Production settings |

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
    ├── Sommerhus.Application
    ├── Sommerhus.Repository
    ├── Sommerhus.Domain
    └── Sommerhus.Contracts

Sommerhus.Mvc
    ├── Sommerhus.Domain
    └── Sommerhus.Contracts

Sommerhus.Repository
    ├── Sommerhus.Application
    └── Sommerhus.Domain

Sommerhus.Application
    ├── Sommerhus.Domain
    └── Sommerhus.Contracts

Sommerhus.Contracts
    └── (none)

Sommerhus.Domain
    └── (none)
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

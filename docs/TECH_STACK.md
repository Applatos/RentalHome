# Sommerhus Technology Stack

## Runtime & Frameworks

| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 8.0 LTS | Runtime and SDK |
| C# | 12.0 | Programming language |
| ASP.NET Core | 8.0 | Web framework (API & MVC) |
| Entity Framework Core | 8.0.10 | ORM / Data access |
| ASP.NET Core Identity | 8.0.10 | User authentication |

---

## Backend (Sommerhus.Api)

### Core Packages
| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.10 | JWT token authentication |
| `Swashbuckle.AspNetCore` | 6.6.2 | Swagger/OpenAPI documentation |
| `Microsoft.EntityFrameworkCore.Design` | 8.0.10 | EF Core tooling |

### Features
- **REST API** with JSON responses
- **JWT Authentication** for admin endpoints
- **Swagger UI** at `/swagger`
- **Static file serving** for uploaded images

---

## Frontend (Sommerhus.Mvc)

### Core Packages
| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.VisualStudio.Web.CodeGeneration.Design` | 8.0.7 | Scaffolding |

### Features
- **Razor Views** with layouts and partials
- **Cookie Authentication** for web sessions
- **Typed HTTP Clients** for API communication
- **Static assets** (CSS, JS, images in `wwwroot/`)

---

## Data Access (Sommerhus.Repository)

### Core Packages
| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.EntityFrameworkCore` | 8.0.10 | Core ORM |
| `Microsoft.EntityFrameworkCore.Sqlite` | 8.0.10 | SQLite provider |
| `Microsoft.EntityFrameworkCore.SqlServer` | 8.0.10 | SQL Server provider |
| `Microsoft.EntityFrameworkCore.Tools` | 8.0.10 | Migrations CLI |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 8.0.10 | Identity with EF Core |

### Database Support
| Database | Use Case |
|----------|----------|
| SQLite | Local development, testing |
| SQL Server | Production deployment |

---

## Testing (Sommerhus.Api.Tests)

### Core Packages
| Package | Version | Purpose |
|---------|---------|---------|
| `xunit` | 2.9.3 | Test framework |
| `xunit.runner.visualstudio` | 3.1.4 | VS test runner |
| `Microsoft.NET.Test.Sdk` | 17.14.1 | Test SDK |
| `Microsoft.AspNetCore.Mvc.Testing` | 8.0.10 | Integration test factory |
| `FluentAssertions` | 8.6.0 | Readable assertions |
| `coverlet.collector` | 6.0.4 | Code coverage |

---

## Development Tools

### Required
| Tool | Purpose |
|------|---------|
| .NET SDK 8.0 | Build and run |
| Git | Version control |
| IDE (VS/Rider/VS Code) | Development |

### Recommended
| Tool | Purpose |
|------|---------|
| Visual Studio 2022 | Full IDE experience |
| JetBrains Rider | Cross-platform IDE |
| VS Code + C# Dev Kit | Lightweight editor |
| Azure Data Studio | Database management |
| Postman / Insomnia | API testing |
| DB Browser for SQLite | SQLite inspection |

### CLI Tools
```powershell
# .NET global tools
dotnet tool install -g dotnet-ef          # EF Core CLI
dotnet tool install -g dotnet-format      # Code formatting
```

---

## CI/CD (GitHub Actions)

### Workflows
| File | Trigger | Purpose |
|------|---------|---------|
| `dotnet.yml` | Push/PR | Build and test |
| `deploy-iis.yml` | Release | Deploy to IIS |
| `smokeTest.yml` | Deploy | Verify deployment |

### Pipeline Steps
1. **Restore** - Download NuGet packages
2. **Build** - Compile solution
3. **Test** - Run xUnit tests
4. **Publish** - Create deployment artifacts
5. **Deploy** - Push to target environment

---

## Configuration

### Secrets Management
| Environment | Method |
|-------------|--------|
| Development | `appsettings.Development.json`, User Secrets |
| Testing | `appsettings.Testing.json`, Environment Variables |
| Production | Environment Variables, Azure Key Vault |

### User Secrets (Local Dev)
```powershell
cd Sommerhus.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "your-secret-key"
dotnet user-secrets set "DefaultAdmin:Password" "your-password"
```

---

## Project Dependencies

```
                    ┌──────────────────┐
                    │  Sommerhus.Api   │
                    └────────┬─────────┘
                             │
         ┌───────────────────┼───────────────────┐
         ▼                   ▼                   ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│   Application   │ │   Repository    │ │    Contracts    │
└────────┬────────┘ └────────┬────────┘ └─────────────────┘
         │                   │
         ▼                   ▼
    ┌─────────────────────────────┐
    │         Domain              │
    └─────────────────────────────┘
```

---

## File Storage

### Local Development
- Images stored in `wwwroot/uploads/{category}/{ownerId}/`
- Categories: `houses/`, `areas/`, `cities/`, `features/`

### Production Considerations
- Consider Azure Blob Storage or AWS S3
- Implement CDN for image delivery
- Add image resizing/optimization

---

## Security

### Authentication Mechanisms
| Layer | Method |
|-------|--------|
| API | JWT Bearer tokens |
| MVC | Cookie authentication |

### JWT Configuration
```json
{
  "Jwt": {
    "Issuer": "Sommerhus.Api",
    "Audience": "Sommerhus.Admin",
    "Key": "[32+ character secret]",
    "AccessTokenMinutes": 60
  }
}
```

### Password Requirements
- Minimum 8 characters
- At least 1 digit
- At least 1 uppercase letter

---

## Monitoring & Logging

### Built-in Logging
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  }
}
```

### Future Considerations
- Application Insights (Azure)
- Seq or ELK stack
- Health check endpoints

---

## Browser Support

### Target Browsers
- Chrome (latest 2 versions)
- Firefox (latest 2 versions)
- Safari (latest 2 versions)
- Edge (latest 2 versions)

### Not Officially Supported
- Internet Explorer (EOL)
- Mobile browsers (not optimized)

---

## Performance Targets

| Metric | Target |
|--------|--------|
| API Response Time | < 200ms (p95) |
| Page Load Time | < 2 seconds |
| Database Query Time | < 100ms |
| Image Upload | < 5 seconds |

---

## Version Compatibility

### Upgrade Path
| Current | Target | Notes |
|---------|--------|-------|
| .NET 8 | .NET 9 | When LTS available |
| EF Core 8 | EF Core 9 | Test migrations |

### Breaking Changes to Watch
- Nullable reference types (enabled)
- Minimal API patterns
- EF Core query behavior changes

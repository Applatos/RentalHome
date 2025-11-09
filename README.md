# Sommerhus Project

## Admin authentication

Both the API and MVC front end rely on a shared `AdminAuth` configuration section. Configure the credentials in `appsettings.json`, environment variables, or user secrets:

```
"AdminAuth": {
  "Username": "admin",
  "Password": "sommerhus123"
}
```

- **Sommerhus.Api** secures `/api/admin/*` endpoints with HTTP Basic authentication. Update `AdminAuth` to control the valid username and password and ensure any external clients send the corresponding `Authorization: Basic ...` header.
- **Sommerhus.Mvc** uses the same credentials for the admin login form and forwards them as Basic auth when calling the API. The MVC site issues its own cookie after a successful login.

For non-development environments, move the credentials into [ASP.NET Core secret storage](https://learn.microsoft.com/aspnet/core/security/app-secrets) or a key vault provider instead of committing them to source control.

## Database configuration

Entity Framework Core now chooses its provider based on the `DatabaseProvider` setting. Supported values are `Sqlite` and `SqlServer`; when omitted, the application inspects the connection string to pick an appropriate provider.

- `appsettings.json` establishes the defaults for local development (SQLite file `sommerhus.db`).
- `appsettings.Development.json`, `appsettings.Testing.json`, and `appsettings.Production.json` each override `ConnectionStrings:Default` so every environment uses its own database.
- Deployment environments can supply the same keys through secrets or environment variables (for example the GitHub Actions workflow sets `DatabaseProvider` and `ConnectionStrings__Default`).

### Running migrations against multiple providers

Use `dotnet ef database update` with the desired connection string to validate migrations on both providers. Examples:

```
# SQLite
dotnet ef database update \
  --project Sommerhus.Repository/Sommerhus.Repository.csproj \
  --startup-project Sommerhus.Api/Sommerhus.Api.csproj \
  --connection "Data Source=./sommerhus.dev.db"

# SQL Server
dotnet ef database update \
  --project Sommerhus.Repository/Sommerhus.Repository.csproj \
  --startup-project Sommerhus.Api/Sommerhus.Api.csproj \
  --connection "Server=localhost;Database=Sommerhus;Trusted_Connection=True;TrustServerCertificate=True"
```

Adjust the connection strings to match your environment (for example `User ID`/`Password` for hosted SQL Server instances).

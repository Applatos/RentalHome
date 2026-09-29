# Sommerhus

A holiday-home rental application: ASP.NET Core 8 + EF Core 8, SQLite locally.

## Projects

| Project | Role |
| --- | --- |
| `Sommerhus.Domain` | Entities only, no dependencies — `VacationHouse`, `Booking`, `City`, pricing types |
| `Sommerhus.Core` | `AppDbContext`, EF migrations, services, DTOs, Identity, image storage |
| `Sommerhus.Api` | JSON API with JWT auth. **The only project that touches the database** |
| `Sommerhus.Mvc` | Razor UI with cookie login. **No database access** — a pure HTTP client of the API |
| `Sommerhus.Api.Tests` | 111 tests against a seeded per-run database |
| `stress-tests/` | k6 scripts and recorded results |
| `docs/` | Architecture, usage, devops, roadmap, known issues |

The split matters when something looks wrong in a page: the MVC app cannot be the cause of a
data problem. Check the API directly first — `http://localhost:5001/swagger`.

## Running it

Both processes, in this order — the UI is useless without the API:

```powershell
dotnet run --project Sommerhus.Api    # http://localhost:5001  (+ /swagger)
dotnet run --project Sommerhus.Mvc    # http://localhost:7202
```

The MVC app finds the API through `Api:BaseUrl`, defaulting to `http://localhost:5001/`. If you
move the API, set that key.

## Authentication

JWT, issued by the API and used by MVC behind its own cookie.

| Purpose | Endpoint |
| --- | --- |
| Admin login | `POST /api/admin/auth/login` — `{ "Username", "Password" }` |
| Guest register | `POST /api/auth/register` — `{ "Email", "Username", "Password" }` |
| Guest login | `POST /api/auth/login` |

The default admin comes from the `DefaultAdmin` section and the signing key from `Jwt:Key`.
The two secrets, `DefaultAdmin:Password` and `Jwt:Key`, are **not** in `appsettings.json`:

- **Development** reads them from `appsettings.Development.json`. Those values are committed,
  public, and only ever meant for a local database.
- **Production** reads them from the API's **server-owned `appsettings.Production.json`**,
  beside the deployed `Sommerhus.Api.dll`. The real production settings and secrets live on
  the IIS server, not in this checkout or the publish package. Missing `Jwt:Key` prevents API
  startup; missing admin credentials skip admin creation. See
  [the deployment guide](docs/DEVOPS.md#production-configuration-belongs-to-the-server).

The demo accounts `owner` and `user` are seeded in Development only.

## The database

Local development uses **one shared SQLite file**:

```
%LOCALAPPDATA%\ApplatosX\SommerhusInterop\applatos.db
```

That is deliberate, and it is probably the surprising part of this repo. The database is shared
with an Applatos model that binds directly to these tables, so a booking created in either
application is visible in the other. Sommerhus owns the schema; Applatos is a guest that never
creates, alters or migrates anything.

`AddSommerhusPersistence` expands environment variables in the connection string, which is why
`appsettings.Development.json` can name a machine-independent location rather than an absolute
path with a user name in it. To work against an isolated database instead:

```powershell
$env:ConnectionStrings__Default = "Data Source=sommerhus.db"
dotnet run --project Sommerhus.Api
```

`DatabaseProvider` selects the EF provider (`Sqlite` or `SqlServer`); when omitted the connection
string is inspected.

### Looking inside it

Use DB Browser for SQLite, applies the write-ahead log automatically.

Two things bite people here:

- **The file is in WAL mode.** Recent changes live in `applatos.db-wal` until a checkpoint, and
  the main file's timestamp does not move. For a filesystem backup, stop every process using
  the database, then copy its directory including any remaining `-wal` and `-shm` files.
  Copying those files separately while writes continue is not a consistent backup.
- **A viewer with *unsaved changes* blocks both applications.** DB Browser keeps a write transaction open until you press *Write Changes*, and while it does, logging in fails with
  `SQLite Error 5: 'database is locked'`, login writes, because Identity updates the user. Press Write Changes, or close it. A viewer that is only *reading* is fine and blocks nothing: WAL mode lets readers and writers run at the same time.

### Startup applies migrations

`MigrationHostedService` in `Sommerhus.Core/ServiceCollectionExtensions.cs` runs before the API serves anything, on **every** start:

1. `db.Database.MigrateAsync()` - applies any pending migration.
2. `ReferenceDataSeeder.SeedCitiesAsync()` - adds missing Danish postal districts from the
   bundled 1,089-entry catalog in **every environment**, including Production. Matches by
   ZIP and preserves existing city IDs, names, descriptions and relationships. No network
   call is needed, and restarting does not duplicate cities.
3. `AdminIdentitySeeder.SeedAsync()` - ensures roles and the configured admin exist; the demo
   owner and user accounts are created only in `Development`.
4. In `Development` only, `Seeder.SeedMinimal(db)` creates demo data (20 features, a house
   and a price calendar), reusing the seeded cities. It skips demo creation if the `Features`
   table is non-empty. The city reference data is independent of that check.

Two consequences worth knowing, because the database is shared:

- **Starting the API can change the schema.** If you check out a branch with a new migration and
  run it against the shared file, that migration is applied. Sommerhus owns the schema so this is
  correct by design, but the Applatos model is bound to specific columns — a migration that
  renames or drops one breaks it, and Applatos will say so at startup rather than silently.
- Running in `Production` seeds the cities and configured admin, but skips demo houses,
  features and accounts. An existing installation with missing cities is repaired at startup.

## Migrations

Twelve migrations, split across **two** folders — `Sommerhus.Core/Migrations/` (the first six) and
`Sommerhus.Core/Data/Migrations/` (the rest). Both are compiled in and both are applied; the split
is historical, not meaningful. `dotnet ef migrations add` writes to `Migrations/` by default, so
pass `-o Data/Migrations` if you want a new one alongside the recent ones.

The EF tools are in `Core` and the design package in `Api`, so both projects have to be named:

```powershell
dotnet ef migrations add <Name> --project Sommerhus.Core --startup-project Sommerhus.Api -o Data/Migrations
dotnet ef database update      --project Sommerhus.Core --startup-project Sommerhus.Api
```

`database update` is rarely needed by hand — startup already migrates.

## Tests

```powershell
dotnet test Sommerhus.Api.Tests
```

They build their own seeded database per run and do not touch the shared file.

## Deploying

The demo hostname is **demo-vac.sima.dk**, hosted on IIS with SQLite. Its target public URL is
**https://demo-vac.sima.dk**. Follow the [win-acme certificate and redirect procedure](docs/DEVOPS.md#https-certificate-and-http-redirection-win-acme)
to complete the server's HTTPS setup before switching the application settings below.
The MVC site runs at `/` and the API as the `/api` application, in separate application pools.
The deployed program folders are under `inetpub`; IIS's **Physical path** is authoritative.

Publish each app locally, then manually copy the contents of its publish folder to the server:

```powershell
dotnet publish .\Sommerhus.Api\Sommerhus.Api.csproj -c Release
dotnet publish .\Sommerhus.Mvc\Sommerhus.Mvc.csproj -c Release
```

Each output is under that project's `bin\Release\net8.0\publish\`. Both projects exclude
`appsettings.Development.json`, `appsettings.Testing.json` and `appsettings.Production.json`
from publish; the API also excludes uploads. **Each deployed app has its own production JSON
file, created and maintained on the server and preserved across releases.** Any Production
files remaining in the checkout are non-deployed reference defaults, not the live settings.
The MVC server file sets `Api:BaseUrl` to `https://demo-vac.sima.dk/api/`; the API server file
sets the database path, admin password and JWT key. The pools select the `Production`
environment. Do not copy server secrets back into source control.

Before an update, stop both pools and back up the database, production files and uploads.
Preserve these when copying new program files. **Do not run the existing deploy/rollback
scripts for this setup:** their `/MIR` can delete the server-only production files, and the
setup script writes environment overrides that take priority over JSON.

The complete manual procedure, server configuration examples, backup/rollback rules and
the filename/path mistakes found during first deployment are in
[`docs/DEVOPS.md`](docs/DEVOPS.md).

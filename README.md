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

One wrinkle in `launchSettings.json`: the MVC profile *named* `https` actually serves
`http://localhost:5015`. Not a typo introduced here — just be aware.

## Authentication

JWT, issued by the API and used by MVC behind its own cookie.

| Purpose | Endpoint |
| --- | --- |
| Admin login | `POST /api/admin/auth/login` — `{ "Username", "Password" }` |
| Guest register | `POST /api/auth/register` — `{ "Email", "Username", "Password" }` |
| Guest login | `POST /api/auth/login` |

The default admin comes from the `DefaultAdmin` section and the signing key from `Jwt:Key`, both
in `appsettings.json`.

> **These are committed to a public repository.** The admin password and the JWT signing key are
> effectively public knowledge. Rotate both, and supply them through environment variables or
> user secrets, before this is reachable by anyone but you.

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

Use DB Browser for SQLite or the `sqlite3` CLI — both apply the write-ahead log automatically.

Two things bite people here:

- **The file is in WAL mode.** Recent changes live in `applatos.db-wal` until a checkpoint, and
  the main file's timestamp does not move. If you copy the database anywhere, take `-wal` and
  `-shm` with it or you will leave the newest rows behind.
- **A viewer with *unsaved changes* blocks both applications.** DB Browser keeps a write
  transaction open until you press *Write Changes*, and while it does, logging in fails with
  `SQLite Error 5: 'database is locked'` — login writes, because Identity updates the user.
  Press Write Changes, or close it. A viewer that is only *reading* is fine and blocks nothing:
  WAL mode lets readers and writers run at the same time.

### Startup applies migrations

`MigrationHostedService` in `Sommerhus.Core/ServiceCollectionExtensions.cs` runs before the API
serves anything, on **every** start:

1. `db.Database.MigrateAsync()` — applies any pending migration.
2. `AdminIdentitySeeder.SeedAsync()` — ensures the admin, owner and user accounts exist.
3. In `Development` only, `Seeder.SeedMinimal(db)` — reference data (the 20 features, a city, a
   house, a price calendar). It returns immediately if the `Features` table is non-empty, so it
   fills an empty database once and never touches a populated one.

Two consequences worth knowing, because the database is shared:

- **Starting the API can change the schema.** If you check out a branch with a new migration and
  run it against the shared file, that migration is applied. Sommerhus owns the schema so this is
  correct by design, but the Applatos model is bound to specific columns — a migration that
  renames or drops one breaks it, and Applatos will say so at startup rather than silently.
- Running in `Production` skips `SeedMinimal` but still migrates and still seeds the admin.

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

## The shared-backend demo

Starting all three processes (API, MVC, and the Applatos host) with the right ports and
configuration is scripted in the ApplatosX repository:

```powershell
.\Prototypes\First\demo\sommerhus\run-demo.ps1 -Seed
```

That directory also holds the provider configuration binding Applatos to these tables, and a
record of what has been verified end to end.

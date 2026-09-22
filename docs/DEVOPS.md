# DevOps Guide — Sommerhus Project

A practical guide for running the project locally and deploying to production. Written so you can follow along even if you've never done DevOps before.

---

## Table of Contents

1. [Quick Reference](#quick-reference)
2. [Local Development Setup](#local-development-setup)
3. [Day-to-Day Development](#day-to-day-development)
4. [Database Changes (Migrations)](#database-changes-migrations)
5. [Deploying to Production](#deploying-to-production)
6. [How the CI/CD Pipeline Works](#how-the-cicd-pipeline-works)
7. [Production Server Setup](#production-server-setup)
8. [Backups](#backups)
9. [Troubleshooting](#troubleshooting)
10. [Recommended Deployment Workflow](#recommended-deployment-workflow)

---

## Quick Reference

| What                  | Development                     | Production                           |
| --------------------- | ------------------------------- | ------------------------------------ |
| **API URL**           | `http://localhost:5001`         | `https://mikkel.smedt.dk`            |
| **MVC URL**           | `http://localhost:7202`         | `https://mikkel.smedt.dk` (MVC site) |
| **Swagger**           | `http://localhost:5001/swagger` | N/A                                  |
| **Database**          | SQLite file (local)             | SQLite file on server                |
| **Database location** | `Sommerhus.Api/app_data/`       | `C:\Data\Sommerhus\api\db\`          |
| **Image uploads**     | `wwwroot/images/` (local disk)  | `C:\WebServer\...\publish\wwwroot\`  |
| **Config file**       | `appsettings.Development.json`  | `appsettings.Production.json`        |

---

## Local Development Setup

### Prerequisites

You need these installed on your machine:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) — the framework the project runs on
- [Git](https://git-scm.com/) — for version control
- A code editor (Visual Studio, VS Code, or Rider)

### First-Time Setup

Open a terminal in the project root folder and run these commands in order:

```powershell
# 1. Download all package dependencies
dotnet restore Sommerhus_project.sln

# 2. Build the entire solution to check for errors
dotnet build Sommerhus_project.sln

# 3. Run the tests to make sure everything works
dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj
```

If all tests pass, you're ready to go.

### Running the Application

You need **two terminals** — one for the API and one for the MVC frontend:

**Terminal 1 — Start the API:**

```powershell
dotnet run --project Sommerhus.Api
```

The API starts at `http://localhost:5001`. You can test it by opening `http://localhost:5001/swagger` in your browser.

**Terminal 2 — Start the MVC frontend:**

```powershell
dotnet run --project Sommerhus.Mvc
```

The website starts at `http://localhost:7202`. Open it in your browser to see the frontend.

> **Tip:** The MVC frontend talks to the API. The API must be running first, or the website will show errors.

### Configuration

Configuration lives in `appsettings.*.json` files. You almost never need to change these for local development.

| File                           | Purpose                                       |
| ------------------------------ | --------------------------------------------- |
| `appsettings.json`             | Shared defaults (both dev and prod)           |
| `appsettings.Development.json` | Local overrides (used when running locally)   |
| `appsettings.Production.json`  | Production settings (used on the live server) |

The API base URL that the MVC app connects to is set in `Sommerhus.Mvc/appsettings.json`:

```json
{ "Api": { "BaseUrl": "http://localhost:5001" } }
```

---

## Day-to-Day Development

### Typical Workflow

1. Pull the latest code: `git pull`
2. Build: `dotnet build Sommerhus_project.sln`
3. Run tests: `dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj`
4. Start API + MVC (two terminals, see above)
5. Make your changes
6. Run tests again before committing
7. Format code: `dotnet format Sommerhus_project.sln`
8. Commit and push

### Before Every Commit

```powershell
dotnet format Sommerhus_project.sln
dotnet build Sommerhus_project.sln --warnaserror
dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj
```

All three must pass with zero warnings and zero failures.

---

## Database Changes (Migrations)

The project uses **Entity Framework Core** with SQLite. The database schema is defined in C# code (the domain models), and EF Core generates "migrations" — small files that describe how to update the database when the schema changes.

### When Do I Need a Migration?

Whenever you change a property on a domain model in `Sommerhus.Domain/Models/`. For example, adding a new field to `VacationHouse`.

### Step-by-Step: Creating a Migration

```powershell
# 1. Make your model changes in code first

# 2. Generate the migration (give it a descriptive name)
dotnet ef migrations add AddMaxGuestsToHouse `
  --project Sommerhus.Core `
  --startup-project Sommerhus.Api

# 3. Review the generated file in Sommerhus.Core/Migrations/
#    Look at the Up() method — does it match what you expect?

# 4. Apply the migration to your local database
dotnet ef database update `
  --project Sommerhus.Core `
  --startup-project Sommerhus.Api

# 5. Run tests to make sure nothing broke
dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj
```

### Undoing a Migration

If you made a mistake and the migration hasn't been deployed to production yet:

```powershell
dotnet ef migrations remove `
  --project Sommerhus.Core `
  --startup-project Sommerhus.Api
```

### Important Rules

- **Never delete a migration** that has already been applied to the production database
- **Always backup the production database** before applying migrations (see [Backups](#backups))
- **Commit migration files** to Git — they are part of the source code
- The app automatically applies pending migrations on startup (`Database.Migrate()`)

### SQLite Limitation

SQLite cannot rename or drop columns easily. EF Core works around this by creating a new table, copying data, and renaming. This is automatic but can be slow on large tables.

---

## Deploying to Production

Production runs on a **Windows Server with IIS** (Internet Information Services). The app is deployed as two separate IIS sites — one for the API, one for the MVC frontend.

### How Deployment Works (Overview)

1. Code is pushed to the `master` branch on GitHub
2. A GitHub Actions workflow automatically builds and deploys to the server
3. The server runs a **self-hosted GitHub runner** that executes the deployment

You can also deploy manually if needed (see below).

### Automatic Deployment (Recommended)

Just push to `master`:

```powershell
git push origin master
```

The GitHub Action (`.github/workflows/deploy-iis.yml`) will:

1. Build the API and MVC projects
2. Put both sites into maintenance mode (`app_offline.htm`)
3. Stop the IIS application pools
4. Copy the new files to the server (preserving database, uploads, and logs)
5. Restart the application pools
6. Remove the maintenance page — sites go live

You can monitor the deployment in the **Actions** tab on GitHub.

### Manual Deployment (If CI/CD Is Down)

Run these commands **on the production server**:

```powershell
# Step 1: Build the projects
dotnet publish Sommerhus.Api -c Release -o C:\_deploy\Sommerhus.Api
dotnet publish Sommerhus.Mvc -c Release -o C:\_deploy\Sommerhus.Mvc

# Step 2: Put sites into maintenance mode
New-Item -Path "C:\WebServer\api_c1_vh_mms.smedt.dk\1.0.0\publish\app_offline.htm" -ItemType File -Force
New-Item -Path "C:\WebServer\mvc_c1_vh_mms.smedt.dk\1.0.0\publish\app_offline.htm" -ItemType File -Force

# Step 3: Stop the application pools
Import-Module WebAdministration
Stop-WebAppPool -Name "SommerhusApiPool"
Stop-WebAppPool -Name "SommerhusMvcPool"

# Step 4: Copy files (this preserves database, uploads, and logs)
robocopy C:\_deploy\Sommerhus.Api C:\WebServer\api_c1_vh_mms.smedt.dk\1.0.0\publish /MIR /XD wwwroot Data logs /XF sommerhus.db
robocopy C:\_deploy\Sommerhus.Mvc C:\WebServer\mvc_c1_vh_mms.smedt.dk\1.0.0\publish /MIR /XD wwwroot\uploads Data logs /XF sommerhus.db

# Step 5: Start the application pools
Start-WebAppPool -Name "SommerhusApiPool"
Start-WebAppPool -Name "SommerhusMvcPool"

# Step 6: Remove maintenance pages
Remove-Item "C:\WebServer\api_c1_vh_mms.smedt.dk\1.0.0\publish\app_offline.htm" -ErrorAction SilentlyContinue
Remove-Item "C:\WebServer\mvc_c1_vh_mms.smedt.dk\1.0.0\publish\app_offline.htm" -ErrorAction SilentlyContinue
```

> **Important:** The `/MIR` flag mirrors the source to the destination. The `/XD` and `/XF` flags exclude persistent folders (database, uploads, logs) so they are NOT overwritten during deployment.

### What Gets Preserved During Deployment

These folders/files on the server are **never overwritten** by a deployment:

- `wwwroot/` — uploaded images
- `Data/` — database folder
- `logs/` — application logs
- `sommerhus.db` — the SQLite database file

---

## How the CI/CD Pipeline Works

The project uses **GitHub Actions** for continuous integration and deployment. You don't need to understand the YAML files in detail — here's what they do:

### Workflows

| File                               | When it runs                | What it does                       |
| ---------------------------------- | --------------------------- | ---------------------------------- |
| `.github/workflows/dotnet.yml`     | Every push and pull request | Builds the code and runs all tests |
| `.github/workflows/deploy-iis.yml` | Push to `master` branch     | Builds and deploys to the server   |
| `.github/workflows/smokeTest.yml`  | Manually triggered          | Tests that the server runner works |

### What Happens When You Push to `master`

1. GitHub detects the push
2. The `deploy-iis.yml` workflow starts on the **self-hosted runner** (a program running on the production server)
3. It builds both projects in Release mode
4. It puts the sites offline, stops IIS, copies files, restarts IIS, and brings sites back online
5. The whole process takes about 2-3 minutes

### What Happens on a Pull Request

1. The `dotnet.yml` workflow runs on GitHub's cloud servers
2. It restores packages, builds, and runs all tests
3. If anything fails, the PR shows a red X — don't merge until it's green

---

## Production Server Setup

This section is for setting up a **new** production server from scratch. You only need this once.

### Prerequisites on the Server

1. **Windows Server** with IIS enabled
2. **[.NET 8 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0)** — this lets IIS run .NET apps
3. **GitHub Actions Runner** — installed as a Windows service (see below)

### Folder Structure on the Server

```
C:\WebServer\
├── api_c1_vh_mms.smedt.dk\1.0.0\publish\    ← API files live here
│   ├── wwwroot\                               ← uploaded images (preserved)
│   ├── Data\                                  ← database folder (preserved)
│   └── logs\                                  ← log files (preserved)
└── mvc_c1_vh_mms.smedt.dk\1.0.0\publish\    ← MVC files live here
    ├── wwwroot\uploads\                       ← user uploads (preserved)
    └── logs\                                  ← log files (preserved)

C:\Data\Sommerhus\api\db\
└── sommerhus.db                               ← the SQLite database
```

### IIS Application Pool Settings

Create two application pools: `SommerhusApiPool` and `SommerhusMvcPool`.

| Setting                 | Value           |
| ----------------------- | --------------- |
| .NET CLR Version        | No Managed Code |
| Managed Pipeline Mode   | Integrated      |
| Start Mode              | AlwaysRunning   |
| Idle Time-out (minutes) | 0 (never stop)  |

### Environment Variables

Set these on the server (via IIS `web.config` or Windows Environment Variables):

| Variable                     | What it does               | Example value                                       |
| ---------------------------- | -------------------------- | --------------------------------------------------- |
| `ASPNETCORE_ENVIRONMENT`     | Tells the app it's in prod | `Production`                                        |
| `ConnectionStrings__Default` | Database connection string | `Data Source=C:\Data\Sommerhus\api\db\sommerhus.db` |
| `Jwt__Key`                   | Secret key for auth tokens | A random string, 32+ characters                     |
| `DefaultAdmin__Password`     | Password of the seeded admin | A strong password; the API refuses to start without it |
| `Jwt__Issuer`                | Who issues the token       | `Sommerhus.Api`                                     |
| `Jwt__Audience`              | Who the token is for       | `Sommerhus.Admin`                                   |

> **Security:** Never put secrets (JWT keys, passwords) in `appsettings.json`. Use environment variables instead.

### Installing the GitHub Actions Runner

The runner is a small program that listens for GitHub deployments and executes them on the server.

1. Go to your GitHub repo → **Settings** → **Actions** → **Runners** → **New self-hosted runner**
2. Follow the instructions to download and configure the runner
3. Install it as a Windows service so it starts automatically:

```powershell
.\config.cmd --url https://github.com/YOUR_ORG/Sommerhus_project --token YOUR_TOKEN
.\svc.cmd install
.\svc.cmd start
```

4. Grant the runner's service account permissions to write to `C:\WebServer\` and `C:\_deploy\`

---

## Backups

### Database Backup

The SQLite database is a single file. Back it up by copying it.

You can schedule this as a **Windows Task Scheduler** job to run daily:

```powershell
# backup-db.ps1 — copy database with today's date
$date = Get-Date -Format "yyyy-MM-dd"
$src  = "C:\Data\Sommerhus\api\db\sommerhus.db"
$dst  = "C:\Backups\Sommerhus\sommerhus_$date.db"

Copy-Item $src $dst

# Delete backups older than 30 days
Get-ChildItem "C:\Backups\Sommerhus\*.db" |
  Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } |
  Remove-Item
```

### Image Backup

Uploaded images live in `wwwroot/` on the server. Back them up with:

```powershell
robocopy "C:\WebServer\api_c1_vh_mms.smedt.dk\1.0.0\publish\wwwroot" "C:\Backups\Images" /MIR
```

---

## Troubleshooting

### Local Development

**"Connection refused" when opening the MVC site**

- The API isn't running. Start it first in a separate terminal: `dotnet run --project Sommerhus.Api`

**"Could not find a part of the path ... sommerhus.db"**

- The database file doesn't exist yet. Run the API once — it creates the database automatically on first startup via `Database.Migrate()`.

**Tests fail after pulling new code**

- Someone may have added a migration. Rebuild and let the app recreate the database:
  ```powershell
  dotnet build Sommerhus_project.sln
  dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj
  ```

**Port already in use**

- Another instance is already running. Find and stop it, or use a different port:
  ```powershell
  dotnet run --project Sommerhus.Api --urls "http://localhost:5184"
  ```

### Production

**"Access denied" during deployment**

- Files are locked because the app is still running. The deployment script should create `app_offline.htm` and stop app pools first. If deploying manually, follow the steps in order.

**Site shows "502 Bad Gateway" or blank page after deploy**

- Check that the app pool started: `Get-WebAppPoolState -Name "SommerhusApiPool"`
- Check the logs: `Get-Content "C:\WebServer\api_c1_vh_mms.smedt.dk\1.0.0\publish\logs\stdout*.log" -Tail 50`

**SQLite "database is locked"**

- Multiple processes are trying to write at the same time. Make sure only one instance of the API is running.

### Useful Commands

```powershell
# Check if the API is responding
Invoke-RestMethod -Uri "http://localhost:5001/api/public/cities"

# Check IIS app pool status (on server)
Import-Module WebAdministration
Get-WebAppPoolState -Name "SommerhusApiPool"
Get-WebAppPoolState -Name "SommerhusMvcPool"

# View recent logs (on server)
Get-Content "C:\WebServer\api_c1_vh_mms.smedt.dk\1.0.0\publish\logs\stdout*.log" -Tail 100
```

---

## Recommended Deployment Workflow

This section describes the recommended way to ship code changes from your local machine to production.

### For Small Changes (Bug Fixes, Config, Copy)

1. Make your changes on a feature branch
2. Run the pre-commit checks locally:
   ```powershell
   dotnet format Sommerhus_project.sln
   dotnet build Sommerhus_project.sln --warnaserror
   dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj
   ```
3. Push and merge directly to `master`
4. The CI/CD pipeline deploys automatically — done in ~3 minutes

### For Larger Changes (New Features, Schema Changes)

1. **Create a feature branch** from `master`:

   ```powershell
   git checkout -b feature/my-new-feature
   ```

2. **Develop and test locally** — run both API and MVC, verify in the browser

3. **If you changed domain models**, create a migration:

   ```powershell
   dotnet ef migrations add MyMigrationName `
     --project Sommerhus.Core `
     --startup-project Sommerhus.Api
   ```

4. **Push your branch and open a Pull Request** on GitHub:

   ```powershell
   git push origin feature/my-new-feature
   ```

   The `dotnet.yml` workflow will automatically build and test your PR. Wait for the green checkmark.

5. **Get a code review** (if working in a team) — have someone look at the PR

6. **Merge the PR into `master`** — this triggers the deployment pipeline

7. **Verify the live site** — open the production URL and check that your changes work

### For Database Migrations in Production

The app automatically applies pending migrations on startup (`Database.Migrate()`). This means:

- When the deployment restarts the app pool, any new migrations in the code will be applied to the production database automatically
- **Always backup the database before deploying migrations** (see [Backups](#backups))
- If a migration fails, the app won't start — check the logs and fix the migration before re-deploying

### For Secrets and Configuration Changes

Never commit secrets to Git. Instead:

1. **Local development**: Use [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets):

   ```powershell
   dotnet user-secrets init --project Sommerhus.Mvc
   dotnet user-secrets set "AdminAuth:Username" "admin" --project Sommerhus.Mvc
   dotnet user-secrets set "AdminAuth:Password" "your-password" --project Sommerhus.Mvc
   ```

2. **Production**: Set environment variables on the server (via IIS `web.config` or Windows Environment Variables). See [Environment Variables](#environment-variables) above.

### Rollback Plan

If a deployment breaks the site:

1. **Check the logs** first — it might be a config issue, not a code issue
2. **Revert the commit** on `master` and push — this triggers a new deployment with the old code:
   ```powershell
   git revert HEAD
   git push origin master
   ```
3. **Restore the database** from backup if a migration caused data issues (see [Backups](#backups))

# DevOps Guide — Sommerhus

How the project is run locally, packaged, put on the production server, and taken back off it.
Written for someone who has not done this before. Every step is a command you can copy.

---

## The whole picture

There are two places the code runs, and nothing in between:

| | Local (your machine) | Production (company IIS server) |
| --- | --- | --- |
| Started by | `dotnet run`, two terminals | IIS, automatically |
| URL | `http://localhost:7202` (MVC), `http://localhost:5001` (API + Swagger) | `https://<host>` (MVC), `https://<host>/api` (API) |
| Environment | `Development` | `Production` |
| Config | `appsettings.Development.json`, committed | Environment variables on the IIS app pools, set once by `setup-server.ps1` |
| Database | SQLite, `%LOCALAPPDATA%\ApplatosX\SommerhusInterop\applatos.db` | SQLite, `C:\Data\Sommerhus\db\sommerhus.db` |
| Demo accounts | `owner` / `user` are seeded | Only `admin` exists |

Shipping is manual and takes about five minutes:

```
1. git push                          (GitHub builds and runs the tests, so a broken commit shows red)
2. .\deploy\publish.ps1              (on your machine: tests, publish, one zip under artifacts\)
3. copy the zip to the server
4. .\deploy.ps1 -Package <zip>       (on the server, as administrator: backup, install, health check)
```

The server has no internet access, so nothing is built or downloaded there. The zip is the only
thing that crosses over, and it contains everything: both apps, the server scripts, and a
`VERSION.txt` naming the commit it was built from.

---

## Local development

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Git
- An editor (Visual Studio, VS Code or Rider)

### Run it

Two terminals. The API first, because the MVC site is a client of it:

```powershell
dotnet run --project Sommerhus.Api     # http://localhost:5001  (Swagger at /swagger)
dotnet run --project Sommerhus.Mvc     # http://localhost:7202
```

Log in with `admin` / `Sommerhus123!`, or as the demo house owner `owner` / `Owner123!`.
Those passwords are in `appsettings.Development.json` and `AdminIdentitySeeder.cs`, they are
public, and they exist only in `Development`.

### Before every commit

```powershell
dotnet build Sommerhus_project.sln --warnaserror
dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj
```

`git push` then runs the same on GitHub (`.github/workflows/dotnet.yml`). A red check on `main`
means do not package that commit.

### Database changes

Change a model under `Sommerhus.Domain/Models`, then:

```powershell
dotnet ef migrations add <Name> --project Sommerhus.Core --startup-project Sommerhus.Api -o Data/Migrations
dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj
```

The API applies pending migrations every time it starts, locally and in production. That is why
`deploy.ps1` backs up the database before it starts the new version. Never delete a migration
that has reached production.

---

## Production server: one-time setup

Do this once per server. Everything below runs **on the server, in an administrator PowerShell**.

1. **Install IIS with the management scripts.** Server Manager → Add Roles → Web Server (IIS),
   or:

   ```powershell
   Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole,IIS-WebServer,IIS-ManagementScriptingTools,IIS-ManagementConsole
   ```

2. **Install the .NET 8 Hosting Bundle.** On a machine with internet, download
   *ASP.NET Core 8.0 Runtime – Windows Hosting Bundle* from
   <https://dotnet.microsoft.com/download/dotnet/8.0>, copy the installer to the server and run
   it. This is what lets IIS host .NET apps. `setup-server.ps1` refuses to run without it.

3. **Get `setup-server.ps1` onto the server.** It is inside every package zip, next to
   `deploy.ps1`, or copy it from `deploy\` in the repository.

4. **Run it.** Pick the site's DNS name and the admin password now:

   ```powershell
   .\setup-server.ps1 -HostName sommerhus.firma.dk -AdminPassword 'Choose1Strong!'
   ```

   Add `-CertThumbprint <thumbprint>` when a certificate for that name is in the machine's
   personal store; the script then adds the https binding and points the MVC site at the API
   over https. Without it the site is http only, which is fine for a first internal test.

   The script creates:

   ```
   C:\Sites\Sommerhus\mvc        IIS site "Sommerhus"            app pool SommerhusMvc
   C:\Sites\Sommerhus\api        IIS application "/api"          app pool SommerhusApi
   C:\Sites\Sommerhus\previous   the version before the current one (for rollback)
   C:\Data\Sommerhus\db          sommerhus.db, created by the API on first start
   C:\Data\Sommerhus\backups     one folder per deploy, last ten kept
   C:\Data\Sommerhus\releases    unpacked packages
   ```

   and puts the configuration on the two app pools as environment variables:

   | Pool | Variable | Value |
   | --- | --- | --- |
   | both | `ASPNETCORE_ENVIRONMENT` | `Production` |
   | SommerhusMvc | `Api__BaseUrl` | `https://<host>/api/` — the slash at the end matters |
   | SommerhusApi | `ConnectionStrings__Default` | `Data Source=C:\Data\Sommerhus\db\sommerhus.db` |
   | SommerhusApi | `DefaultAdmin__Password` | what you passed |
   | SommerhusApi | `Jwt__Key` | generated randomly; nobody needs to know it |

   **Why the app pool and not `web.config`?** Every deploy replaces `web.config`. The pool's
   variables live in IIS's own configuration and survive. To see or change them later: IIS
   Manager → Application Pools → the pool → Advanced Settings → Environment Variables. Both
   the password and the key are required; the API refuses to start without them rather than
   run with an empty signing key.

   Safe to run again: it updates what exists and never creates duplicates.

5. **Deploy the first package** (next section). Until then the site answers 403/404, because
   the folders are empty.

### Why `/api/api/...`

The API is an IIS application at `/api`, and its own routes also start with `api/`. So the
public URL of, say, the cities endpoint is `https://<host>/api/api/cities`. The MVC site is
built for exactly that (`Api__BaseUrl` ends in `/api/`), and the health check in `deploy.ps1`
uses that path. It looks odd, but it is one hostname, one certificate, and no CORS.

---

## Shipping a change

### 1. Package, on your machine

```powershell
.\deploy\publish.ps1
```

This runs the test suite (a failing test stops the packaging), publishes both projects in
Release, removes the Development settings, and writes
`artifacts\sommerhus-<yyyyMMdd-HHmm>-<commit>.zip`. It warns if you have uncommitted changes,
because the version in the zip would not match the commit it names.

`-SkipTests` exists for iterating on the packaging itself. Do not ship with it.

### 2. Copy the zip to the server

Any way that works for you: RDP clipboard, a file share, a USB stick. Put it in
`C:\Data\Sommerhus\releases\` to keep them together.

### 3. Install, on the server

Administrator PowerShell:

```powershell
cd C:\Data\Sommerhus\releases
Expand-Archive .\sommerhus-20260922-1530-390f1e3.zip -DestinationPath .\unpacked -Force   # only to get deploy.ps1 the first time
.\unpacked\deploy.ps1 -Package .\sommerhus-20260922-1530-390f1e3.zip
```

After the first deploy, `deploy.ps1` already sits in `C:\Sites\Sommerhus\` next to `CURRENT.txt`
if you copy it there once; any copy of the script works, it has no state of its own.

What it does, in order, and prints as it goes:

1. Unpacks the zip under `releases\`.
2. Drops `app_offline.htm` into both sites (visitors see "opdateres") and stops both pools.
3. **Backs up** `sommerhus.db` (with its `-wal`/`-shm` files) and the uploaded images to
   `C:\Data\Sommerhus\backups\<timestamp>\`.
4. Copies the currently running version to `C:\Sites\Sommerhus\previous\`.
5. Mirrors the new files in. `wwwroot\uploads` and `logs` are never touched.
6. Starts the pools and polls `/` and `/api/api/cities` for up to a minute.
7. Removes `app_offline.htm` only when both answer.

If step 6 fails the sites stay offline, and the script names the log files and tells you to run
`rollback.ps1`. It never rolls back on its own.

### 4. Check

Open the site, log in as `admin`, open a house. `C:\Sites\Sommerhus\CURRENT.txt` says which
commit is live.

---

## Rolling back

```powershell
.\rollback.ps1
```

Stops the pools, copies `previous\` back over the live folders, starts the pools. Takes seconds.
Uploads are untouched.

**The database is not rolled back.** Usually that is right: the old code runs fine on a newer
schema. If the deploy you are undoing added a migration that broke the data, restore the
database by hand before running the old version:

```powershell
Stop-WebAppPool SommerhusApi
Copy-Item C:\Data\Sommerhus\backups\<timestamp>\db\* C:\Data\Sommerhus\db\ -Force
Start-WebAppPool SommerhusApi
```

Every deploy prints its backup folder; it is also the newest folder under `backups\`.

---

## Where things are on the server

| What | Where |
| --- | --- |
| Live MVC files | `C:\Sites\Sommerhus\mvc\` |
| Live API files | `C:\Sites\Sommerhus\api\` |
| Which version is live | `C:\Sites\Sommerhus\CURRENT.txt` |
| Uploaded images | `C:\Sites\Sommerhus\api\wwwroot\uploads\` (never overwritten by a deploy) |
| Application logs | `C:\Sites\Sommerhus\{api,mvc}\logs\stdout*.log` |
| Database | `C:\Data\Sommerhus\db\sommerhus.db` |
| Backups | `C:\Data\Sommerhus\backups\<timestamp>\` |
| Configuration and secrets | IIS → Application Pools → SommerhusApi / SommerhusMvc → Environment Variables |

### Regular backups

`deploy.ps1` backs up on every deploy, but a site that is not deployed for a month has a
month-old backup. Schedule this daily in Task Scheduler (run as SYSTEM, highest privileges):

```powershell
# C:\Data\Sommerhus\backup-daily.ps1
$stamp = Get-Date -Format 'yyyyMMdd'
$dst = "C:\Data\Sommerhus\backups\daily-$stamp"
New-Item -ItemType Directory -Path "$dst\db" -Force | Out-Null
Copy-Item C:\Data\Sommerhus\db\sommerhus.db* "$dst\db"
robocopy C:\Sites\Sommerhus\api\wwwroot\uploads "$dst\uploads" /MIR /NFL /NDL /NJH /NJS | Out-Null
Get-ChildItem C:\Data\Sommerhus\backups -Directory -Filter 'daily-*' | Sort-Object Name -Descending | Select-Object -Skip 30 | Remove-Item -Recurse -Force
```

Copying the `.db` while the API runs is safe enough for a nightly backup: SQLite writes are
atomic and the `-wal` file is taken along. Copy the whole `backups\` folder off the server now
and then; a backup on the same disk is not a backup against the disk.

---

## Troubleshooting

**The site shows the "opdateres" page and nothing else.**
A deploy failed its health check. Read `C:\Sites\Sommerhus\api\logs\stdout*.log` (newest file);
the first lines after startup say why. Then either fix and deploy again, or `rollback.ps1`.

**502.5 / 500.30 right after a deploy.**
The app did not start. Almost always one of: the Hosting Bundle is missing or older than .NET 8;
a required environment variable is missing on the pool (`Jwt__Key`, `DefaultAdmin__Password`,
`Api__BaseUrl` — the log names the one); the pool account cannot write to `logs\` or `db\`
(re-run `setup-server.ps1`, it fixes permissions).

**Pages load but every list is empty or shows "Network error".**
The MVC site cannot reach the API. Check `Api__BaseUrl` on the SommerhusMvc pool: it must be the
public address with `/api/` at the end, and the server must be able to reach itself on its
own public name. Test from the server: `Invoke-WebRequest http://<host>/api/api/cities`. If DNS
resolves but the connection times out (a VM behind NAT often cannot reach its own public IP),
add a line to `C:\Windows\System32\drivers\etc\hosts` so the name points at the server itself:

```
127.0.0.1   <host>
```

**Images do not show.**
The API builds image URLs from the address it was called on. If `Api__BaseUrl` points at
`localhost`, browsers get `localhost` links. Use the public hostname.

**"database is locked".**
Two processes are writing to the SQLite file. Only the SommerhusApi pool should ever open it;
close any DB browser tool that has unsaved changes.

**Login stops working after an hour.**
Expected: the session lasts as long as the API token (60 minutes, `Jwt:AccessTokenMinutes`).
Log in again.

---

## Later: automating the deploy

Everything above is designed so that automation is a small step, not a rewrite: a GitHub
Actions job that runs `publish.ps1` and then `deploy.ps1` on a self-hosted runner is about
twenty lines. It needs the server to reach GitHub over HTTPS (outbound only). Until it can, the
manual path is the deploy path, and the old runner-based workflow is in git history
(`.github/workflows/deploy-iis.yml`, removed 2026-09-22) if you want a starting point.

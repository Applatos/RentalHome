# DevOps Guide — Sommerhus

The current RentalHome demo runs at **http://demo_vac.sima.dk** on the company IIS server.
Deployment is manual: publish both apps locally, transfer the files, and copy them into their
IIS application directories. HTTPS is deferred for this synthetic-data demo.

**Production configuration belongs to the server.** Each deployed app has its own
`appsettings.Production.json`, created on the server and preserved during releases. The
checkout and publish output do not contain the live production settings or secrets.
Read [the script compatibility note](#existing-deployment-scripts) before using anything
under `deploy/`: those scripts implement the previous deployment convention.

## The current layout

| Concern | Local development | IIS demo |
| --- | --- | --- |
| MVC | `http://localhost:7202` | `http://demo_vac.sima.dk/` |
| API application | `http://localhost:5001` | `http://demo_vac.sima.dk/api/` |
| Environment | `Development` | `Production` |
| Environment-specific configuration | Local `appsettings.Development.json` | One server-owned `appsettings.Production.json` per app |
| Database | SQLite, configured locally | Separate SQLite file outside the application directories |
| Startup | Two `dotnet run` processes | IIS pools `SommerhusMvc` and `SommerhusApi` |
| Deployment | Build on the development machine | Manually copy the published files |

The deployed application directories are under **inetpub**. Always check **IIS Manager →
site/application → Basic Settings → Physical path** for the actual directory.
The examples below use `C:\inetpub\Sommerhus\mvc` and `C:\inetpub\Sommerhus\api`;
they are path examples, not a reason to move an existing working installation.

The database example is `C:\Data\Sommerhus\db\sommerhus.db`, with backups under
`C:\Data\Sommerhus\backups`. The server API's `ConnectionStrings:Default` determines
the real database path. Create the directory named there; `C:\Data\db` and
`C:\Data\Sommerhus\db` are different locations.

MVC is an HTTP client of the API. Only the API opens the database. IIS supplies the first
`/api` path segment, and the API's own controller routes supply the second, so the cities
endpoint is **http://demo_vac.sima.dk/api/api/cities**. Swagger is at
**http://demo_vac.sima.dk/api/swagger**.

## Production configuration belongs to the server

The project files already set `CopyToPublishDirectory="Never"` for:

- `appsettings.Development.json`
- `appsettings.Testing.json`
- `appsettings.Production.json`

The API also excludes `wwwroot\uploads\**\*` from publish. Keep these exclusions when
changing packaging. The shared `appsettings.json`, binaries, `web.config` and static
application assets are published.

Any `appsettings.Production.json` still present in the source checkout is non-deployed
reference configuration. It is not a copy of the live server file. Do not copy real server
passwords or keys into it, remove the publish exclusion, or infer live values from it.

On the server, put each production file **beside that app's DLL and web.config**, outside
`wwwroot`. The actual filename must be **appsettings.Production.json**, exactly once.
Enable **View → File name extensions** in Explorer: when extensions are hidden, Explorer
can display this file as `appsettings.Production`. Adding another extension can leave
`appsettings.Production.json.json` or `appsettings.Production.json.txt`, neither of which
is loaded as the production settings file.

### MVC production file

Create `<mvc-path>\appsettings.Production.json`:

```json
{
  "Api": {
    "BaseUrl": "http://demo_vac.sima.dk/api/"
  }
}
```

Use the public hostname and the application path. The API also builds image URLs from the
address it was called on, so a localhost address can produce unusable links in users' browsers.

### API production file

Create `<api-path>\appsettings.Production.json` using the actual database location:

```json
{
  "DatabaseProvider": "Sqlite",
  "ConnectionStrings": {
    "Default": "Data Source=C:/Data/Sommerhus/db/sommerhus.db"
  },
  "DefaultAdmin": {
    "Password": "REPLACE_WITH_PRIVATE_DEMO_PASSWORD"
  },
  "Jwt": {
    "Key": "REPLACE_WITH_GENERATED_KEY"
  }
}
```

Replace both placeholders before starting. Use a unique demo password with at least 16
characters, upper/lower case, a digit and a symbol. The base `appsettings.json` already
supplies the `admin` username, email and JWT issuer, audience and lifetime.

Generate the signing key once in PowerShell on the server:

```powershell
$jwtBytes = New-Object byte[] 48
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($jwtBytes)
[Convert]::ToBase64String($jwtBytes)
```

Paste the result into `Jwt:Key` and preserve it across releases. Do not put the key in Git
or diagnostic messages. Forward slashes in the example database path are valid on Windows;
backslashes inside JSON strings must be escaped as `\\`.

### Environment and precedence

Both IIS pools select `ASPNETCORE_ENVIRONMENT=Production`. `Release` is a build
configuration; it does not select the runtime environment. If `DOTNET_ENVIRONMENT` is also
configured, keep it consistent with `Production`.

The relevant precedence is base `appsettings.json`, then `appsettings.Production.json`,
then environment variables, with later values overriding earlier ones. This setup stores
application values in the server JSON files. If an earlier setup added `Api__BaseUrl`,
`DatabaseProvider`, `ConnectionStrings__Default`, `DefaultAdmin__Password` or
`Jwt__Key` to the pools, remove those obsolete overrides from the affected pools.
A correct JSON file cannot override a stale environment variable.

To set the pool environment in IIS Manager, select the **server node → Configuration
Editor → system.applicationHost/applicationPools → (Collection)**. Select the individual
pool, open its **environmentVariables** collection, add the name/value pair and **Apply**.
Stop and start the affected pool after changing its environment.

## One-time IIS setup

1. Confirm IIS and the **.NET 8 Hosting Bundle** are installed. On the server, run
   `dotnet --list-runtimes`: both `Microsoft.NETCore.App 8.0.x` and
   `Microsoft.AspNetCore.App 8.0.x` must be present. A different major runtime alone is
   insufficient. The server does not need the SDK or source code.
2. Create the two application directories, each with a `logs` subdirectory. Create
   `<api-path>\wwwroot\uploads`, the database parent directory and the backup directory.
   Keep database and backup files outside the served application directories.
3. Create pools `SommerhusMvc` and `SommerhusApi`. Use **No Managed Code**, **Integrated**,
   **ApplicationPoolIdentity**, **Enable 32-Bit Applications = False** for this x64 setup,
   and **Load User Profile = True**. The latter supports persistent Data Protection keys
   with the default `setProfileEnvironment=true`. Set the Production environment as above.
4. Grant the permissions in the table below. In Explorer use **Properties → Security →
   Edit → Add**, select the server under **Locations**, and enter the pool account name.
   Apply the permissions to child folders and files.
5. Create site `Sommerhus`, physical path `<mvc-path>`, pool `SommerhusMvc`, binding
   **http**, port **80**, host **demo_vac.sima.dk**. Add an **Application** beneath it
   with alias `api`, physical path `<api-path>`, and pool `SommerhusApi`. Each
   in-process app needs its own pool.
6. Enable **Anonymous Authentication** for the applications; RentalHome handles user login.
   DNS must lead to the server, and the network must allow TCP port 80.
7. Copy the published files and create both server production files before first use.
   Follow the publish and verification steps below.

| Directory | Account | Permission |
| --- | --- | --- |
| `<mvc-path>` | `IIS AppPool\SommerhusMvc` | Read & execute |
| `<api-path>` | `IIS AppPool\SommerhusApi` | Read & execute |
| `<mvc-path>\logs` | `IIS AppPool\SommerhusMvc` | Modify |
| `<api-path>\logs` | `IIS AppPool\SommerhusApi` | Modify |
| `<api-path>\wwwroot\uploads` | `IIS AppPool\SommerhusApi` | Modify |
| Database parent directory | `IIS AppPool\SommerhusApi` | Modify |

The API needs directory access for SQLite's database, WAL and SHM files.
See [Microsoft's IIS hosting guidance](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/)
for the hosting and application-pool settings.

## Publish and transfer from the development machine

Run commands from the RentalHome repository root, one at a time.

1. Test the release:

   ```powershell
   dotnet test .\Sommerhus.Api.Tests\Sommerhus.Api.Tests.csproj -c Release
   ```

2. For a fresh package, remove only the two generated local publish directories in Explorer:

   ```text
   Sommerhus.Api\bin\Release\net8.0\publish
   Sommerhus.Mvc\bin\Release\net8.0\publish
   ```

   This matters after changing exclusions: an old file can remain in existing output even
   when the next publish no longer copies it.

3. Publish both projects:

   ```powershell
   dotnet publish .\Sommerhus.Api\Sommerhus.Api.csproj -c Release
   dotnet publish .\Sommerhus.Mvc\Sommerhus.Mvc.csproj -c Release
   ```

   These commands recreate the default publish directories listed above. No custom
   `artifacts` directory, `-o` option or deployment script is required.

4. Check both outputs contain `appsettings.json` and `web.config`, but no Development,
   Testing or Production settings. Check the API output contains no local upload payloads.
   Record the source commit for the release, and whether it includes uncommitted changes.
5. Transfer both outputs through RDP file copy or an approved share. Zipping them first is
   optional. For an update, transfer into a temporary server directory before stopping apps.
   Copy the **contents** of the API publish directory to `<api-path>`, and MVC contents to
   `<mvc-path>`, including `runtimes` and static asset subdirectories. Do not leave an extra
   `publish` directory between the IIS physical path and the DLL.

For an existing installation, perform the stopped-pool backup and copy sequence in the
next section before copying into the live directories.

## Updating an existing installation

1. Prepare and transfer a tested release as above.
2. Stop **both** `SommerhusMvc` and `SommerhusApi` in IIS Manager. Keep them stopped
   throughout backup and copying.
3. Create a dated backup directory outside the application directories. Copy both current
   application directories, including their server production files and API uploads. Also
   copy the complete database directory, including any remaining `-wal` and `-shm` files.
   Record which source version the backup represents. On the first deployment there is no
   previous installation to back up.
4. Copy the new publish contents into the existing live directories. Use ordinary copy,
   preserving destination files absent from the package. Preserve both server
   `appsettings.Production.json` files, `wwwroot\uploads`, logs and the external database.
   Do not delete the whole application directories or use mirror/purge copying.
   If the release deliberately removes an application file, remove that specific obsolete
   file after backup; an ordinary overlay does not remove it automatically.
5. Start both pools and the site. Run the checks below.

The existing `deploy.ps1` and `rollback.ps1` do not implement this preservation rule.
Do not substitute them for these steps.

## First start and verification

The API runs EF migrations before serving requests on **every startup**. It creates roles
and a configured admin account if needed. It does not generate demo houses, cities or
features in Production, nor the Development-only `owner` and `user` accounts.

Missing `Jwt:Key` fails startup validation. Missing admin credentials skip admin creation
with a warning; password-validation failures are logged. Changing `DefaultAdmin:Password`
later does not reset an existing admin's password.

1. Request **http://demo_vac.sima.dk/api/api/cities** from the server itself and from a client.
   `[]` is a healthy result for a new empty database.
2. Open **http://demo_vac.sima.dk/** or `/houses`. MVC must call the public API address,
   not `localhost:5001`.
3. Log in at `/account/login` as `admin` with the server-configured password. Test creating
   demo data and uploading an image.
4. Stop and start the pools, then verify the data and image are still available.

## Backups and rollback

Take a backup **before starting a new release**, because its startup can change the schema.
Protect the backups like the server configuration: they contain credentials and signing keys.

For a filesystem backup, stop the pools and any other process using the database, copy the
complete database directory and uploads, then start again. An ordinary copy of a live
database plus its WAL/SHM files is not a reliable snapshot. For online backup automation,
use SQLite's [backup API](https://www.sqlite.org/backup.html) or a tool implementing it.
Keep a backup outside the server as well.

For rollback, stop both pools and restore the previous application files from the recorded
backup, preserving the server configuration and uploads. Code rollback does not undo
migrations: check compatibility before starting old code against the current schema.
If the database must also be restored, preserve the failed state separately first, then
restore a consistent backed-up database file set into a cleared database directory while
all users of it are stopped. Do not combine an old database with WAL/SHM files from the
failed release. Restoring an older database loses changes made since that backup.

## Troubleshooting

### MVC shows "Network error ... localhost:5001"

MVC has started, but its effective API address is still the local default from
`appsettings.json`. In IIS, use **Basic Settings / Explore** to find the directory actually
served. Check that its production file is beside `Sommerhus.Mvc.dll`, has the exact
filename, and sets `Api:BaseUrl` to `http://demo_vac.sima.dk/api/`. Check the pool's
environment and any higher-priority overrides. Stop/start the pool after correcting them.

To show actual filenames without revealing configuration values, run this from the deployed
application directory:

```powershell
Get-ChildItem -Name appsettings*
```

### API returns 500.30 and there is no API log

500.30 means startup failed; it does not identify the cause. Check the API's production
filename/location and JWT key, then the configured database directory and its permissions.

The deployed `web.config` controls stdout logging using `stdoutLogEnabled` and
`stdoutLogFile=".\logs\stdout"`. If enabled, the API pool must be able to write to its
own logs directory. A working MVC log says nothing about API permissions.

Reproduce the API request, then open **Win+R → eventvwr.msc → Windows Logs → Application**.
Read the newest relevant **IIS AspNetCore Module V2** or **.NET Runtime** error, matching
the API path and request time. Share the exception with secrets removed. A direct DLL launch
runs under a different Windows identity/environment and can run migrations; it is not proof
that IIS permissions are correct.

The checked-in web.config templates enable stdout logs for diagnosis. Disable
`stdoutLogEnabled` after troubleshooting; these logs are not automatically rotated, and
a subsequent publish copies the checked-in template settings again.

### The two problems encountered during first deployment

- Incorrect production filenames prevented the intended configuration from being loaded.
  The fix was the exact physical filename `appsettings.Production.json`; Explorer's hidden
  extension made the displayed name misleading.
- The database directory was created at a different path from the connection string.
  Moving/creating it at the configured location fixed the remaining startup problem.
  Neither `inetpub` nor `Sommerhus` is a special application requirement; matching paths
  and access permissions are what matter.

### The API works externally but MVC cannot reach it

Test `http://demo_vac.sima.dk/api/api/cities` from the server. If public-IP loopback/NAT
prevents it reaching itself, use internal DNS or a hosts entry pointing the same hostname to
the server's reachable local address. Keep the hostname in `Api:BaseUrl` so image links
remain usable by browsers.

### Images fail, the database is locked, or login expires

Check Modify access to API `wwwroot\uploads` for uploads, and the public API base address
for image links. For a locked database, close tools holding unsaved write transactions.
The API token normally lasts 60 minutes (`Jwt:AccessTokenMinutes`); log in again after expiry.

## Existing deployment scripts

These scripts remain in the repository but are **not the current demo deployment workflow**:

| Script | Difference from the current setup |
| --- | --- |
| `deploy/publish.ps1` | Older test/publish/ZIP wrapper; unnecessary for the plain publish workflow. |
| `deploy/setup-server.ps1` | Defaults to `C:\Sites\Sommerhus` and writes pool environment values that override server JSON. |
| `deploy/deploy.ps1` | Uses `robocopy /MIR` without preserving server-only production files. |
| `deploy/rollback.ps1` | Also mirrors files without preserving server-only production files. |

Do not run the setup, deploy or rollback scripts against this installation unchanged.
Any future automation must preserve server configuration, uploads and the database, back up
before migrations, and verify the API and MVC separately. Do not infer that a checked-in
script describes the live setup.

## Local development and CI

Use a .NET 8-capable SDK locally. Start the API and MVC in separate terminals:

```powershell
dotnet run --project Sommerhus.Api
dotnet run --project Sommerhus.Mvc
```

Development credentials are the public local-only values in Development configuration.
For schema changes, keep every migration that has reached the server:

```powershell
dotnet ef migrations add <Name> --project Sommerhus.Core --startup-project Sommerhus.Api -o Data/Migrations
dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj
```

The GitHub workflow `.github/workflows/dotnet.yml` restores, builds and tests on pushes and
pull requests to `main`. Deployment remains manual; no current workflow installs releases
on IIS.

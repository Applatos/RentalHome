# DevOps Guide — Sommerhus

The RentalHome demo's hostname is **demo-vac.sima.dk** on the company IIS server.
Deployment is manual: publish both apps locally, transfer the files, and copy them into their
IIS application directories. The target public address is **https://demo-vac.sima.dk**.
Complete [the win-acme HTTPS setup](#https-certificate-and-http-redirection-win-acme) on the
server before using the HTTPS configuration below. This guide does not certify that the
server migration has been completed.

**Production configuration belongs to the server.** Each deployed app has its own
`appsettings.Production.json`, created on the server and preserved during releases. The
checkout and publish output do not contain the live production settings or secrets.
Read [the script compatibility note](#existing-deployment-scripts) before using anything
under `deploy/`: those scripts implement the previous deployment convention.

## Deployment layout after HTTPS setup

| Concern | Local development | IIS demo |
| --- | --- | --- |
| MVC | `http://localhost:7202` | `https://demo-vac.sima.dk/` |
| API application | `http://localhost:5001` | `https://demo-vac.sima.dk/api/` |
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
endpoint is **https://demo-vac.sima.dk/api/api/cities**. Swagger is at
**https://demo-vac.sima.dk/api/swagger**.

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
    "BaseUrl": "https://demo-vac.sima.dk/api/"
  }
}
```

Use the public hostname and the application path. The API also builds image URLs from the
address it was called on, so a localhost address can produce unusable links in users' browsers.
During initial HTTP setup, keep the `http://` address until the certificate and HTTPS binding
work. Then switch to the value above before enabling redirection.

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
   **http**, port **80**, host **demo-vac.sima.dk**. Add an **Application** beneath it
   with alias `api`, physical path `<api-path>`, and pool `SommerhusApi`. Each
   in-process app needs its own pool.
6. Enable **Anonymous Authentication** for the applications; RentalHome handles user login.
   DNS must lead to the server, and the network must allow TCP ports 80 and 443.
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

## HTTPS certificate and HTTP redirection (win-acme)

The server already has win-acme (`wacs.exe`). Certificate issuance, renewal and HTTP
redirection belong to IIS/server configuration; this change needs no application publish.
Keep the two application pools and the existing `/api` application.

### 1. Issue and bind the certificate

1. Confirm the site's HTTP binding is `demo-vac.sima.dk` on port 80 and that DNS leads to
   this server. Ports 80 and 443 must be reachable externally.
2. Run the installed `wacs.exe` as Administrator. Choose **N** (new certificate with default
   settings), select the RentalHome IIS site, and include only **demo-vac.sima.dk**. Use
   HTTP validation with the default self-hosting plugin and the IIS installation step.
3. Confirm issuance succeeds. In IIS **Bindings**, verify HTTPS on port **443**, hostname
   **demo-vac.sima.dk**, SNI enabled for the shared server, and the new certificate selected.
4. Open `https://demo-vac.sima.dk/api/api/cities` and `https://demo-vac.sima.dk/houses`
   from a client and the server. Both must work with a trusted certificate before proceeding.

The client creates/updates the HTTPS binding and remembers the installation for renewals.
See [win-acme getting started](https://www.win-acme.com/manual/getting-started) and
[IIS installation](https://www.win-acme.com/reference/plugins/installation/iis).

### 2. Switch MVC's API calls to HTTPS

In the server's MVC `appsettings.Production.json`, set `Api:BaseUrl` to
`https://demo-vac.sima.dk/api/`. Preserve the other settings. Remove/update any overriding
`Api__BaseUrl` environment variable, then stop/start `SommerhusMvc`. Verify login, the
house list and images. Image URLs should now start with `https://demo-vac.sima.dk/api/uploads/`.
Authenticated API calls must use HTTPS directly; do not rely on following an HTTP redirect
with a bearer token.

### 3. Enable site-level HTTP to HTTPS redirection

Use IIS's native site HSTS settings on **IIS 10 version 1709 or later**. They cover the
site and its `/api` application and are stored in `applicationHost.config`, so publishing
either app's `web.config` does not overwrite them. The destination is HTTPS on port 443.
See [Microsoft's site HSTS reference](https://learn.microsoft.com/en-us/iis/configuration/system.applicationhost/sites/site/hsts).

After HTTPS works, run in Administrator PowerShell on the server. Replace `Sommerhus` if
the actual **IIS site name** differs (it is not the hostname or application-pool name):

```powershell
$iisAppCmd = Join-Path $env:windir 'System32\inetsrv\appcmd.exe'
& $iisAppCmd add backup ("RentalHome-before-https-" + (Get-Date -Format 'yyyyMMdd-HHmmss'))
```

After the backup succeeds, apply the settings to that site:

```powershell
$httpsSettings = @(
    'set', 'config', '-section:system.applicationHost/sites',
    "/[name='Sommerhus'].hsts.enabled:True",
    "/[name='Sommerhus'].hsts.max-age:86400",
    "/[name='Sommerhus'].hsts.includeSubDomains:False",
    "/[name='Sommerhus'].hsts.preload:False",
    "/[name='Sommerhus'].hsts.redirectHttpToHttps:True",
    '/commit:apphost'
)
& $iisAppCmd @httpsSettings
```

The initial HSTS duration is one day. Once HTTPS and renewal are proven, it can be increased.
If IIS reports that `hsts` is unrecognized, this native option is unavailable on that server;
use an IIS URL Rewrite rule configured for the whole site instead, after checking the
installed module/version. Keep redirect configuration in the server's `applicationHost.config`
so it also covers `/api` and survives publish.

### 4. Verify redirects and automatic renewal

Run these on the server and on a client (`curl.exe` avoids PowerShell's `curl` alias):

```powershell
curl.exe -I "http://demo-vac.sima.dk/houses?query=pool"
curl.exe -I "http://demo-vac.sima.dk/api/api/cities"
curl.exe -I "https://demo-vac.sima.dk/houses"
```

The HTTP requests must return a redirect with an HTTPS `Location` preserving the path and
query. HTTPS must serve the page without a loop and include `Strict-Transport-Security`.

Keep the HTTP port-80 binding and firewall access for certificate validation and redirects.
The default win-acme self-hosting validator temporarily shares port 80 with IIS. Verify the
existing win-acme scheduled task is enabled and still points to its permanent installation.
After enabling redirects, use win-acme's renewal management to test renewal of **only this
certificate**, and verify the updated IIS binding and successful validation in its log.
A newly issued certificate may reuse cached authorization: only a log showing an actual
HTTP-01 challenge proves that validation was exercised with the redirect active. Monitor
subsequent scheduled renewals. See [self-hosting validation](https://www.win-acme.com/reference/plugins/validation/http/selfhosting),
[automatic renewal](https://www.win-acme.com/manual/automatic-renewal) and
[Let's Encrypt's port-80 guidance](https://letsencrypt.org/docs/allow-port-80/).

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

The API runs EF migrations before serving requests on **every startup**. It then adds missing
Danish cities/postal districts from the bundled 1,089-entry catalog and creates roles and a
configured admin account if needed. Cities are required reference data in **Production** too:
house creation needs a city to select. Seeding matches by ZIP and preserves existing IDs,
names, descriptions and relationships; it works even if features or other data already exist.
No external postal-code API, database reset or Development environment is required.
Demo houses and features, and the Development-only `owner` and `user` accounts, are not
created in Production.

Missing `Jwt:Key` fails startup validation. Missing admin credentials skip admin creation
with a warning; password-validation failures are logged. Changing `DefaultAdmin:Password`
later does not reset an existing admin's password.

1. Request **https://demo-vac.sima.dk/api/api/cities** from the server itself and from a client.
   It must include the bundled postal districts, for example `6857` / `Blåvand`.
   `[]` is a failure: check that the updated API was deployed and its pool restarted.
2. Open **https://demo-vac.sima.dk/** or `/houses`. MVC must call the public API address,
   not `localhost:5001`.
3. Log in at `/account/login` as `admin` with the server-configured password. Create a house
   using a seeded city. Upload a small PNG or JPEG to a feature and verify the image loads
   immediately, before recycling either pool.
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

### No cities to select when creating a house

Older releases seeded cities only in Development, leaving a fresh Production database empty.
Publish and deploy the updated API and MVC using the normal stopped-pool backup procedure
above, preserving the database and server settings. On API startup, the missing postal
districts are added automatically. Existing data is preserved. Check
`https://demo-vac.sima.dk/api/api/cities` and the house form's city list after startup.
Do not change the server to Development or delete the database to obtain cities.

### MVC still calls the old hostname after an IIS binding change

Changing the IIS binding does not change MVC's configured API address. If the page loads at
`demo-vac.sima.dk` but reports "No such host is known" for the old hostname, edit the
server-owned `appsettings.Production.json` beside `Sommerhus.Mvc.dll`. Set `Api:BaseUrl` to
`https://demo-vac.sima.dk/api/` after HTTPS setup, including the trailing slash. Remove or update any stale
`Api__BaseUrl` environment variable, which overrides JSON, then recycle the `SommerhusMvc`
application pool. No publish is needed for this server configuration change. Request
`https://demo-vac.sima.dk/api/api/cities` from the server itself to verify that the new hostname
resolves there and reaches the API, then reload the houses page.

### MVC shows "Network error ... localhost:5001"

MVC has started, but its effective API address is still the local default from
`appsettings.json`. In IIS, use **Basic Settings / Explore** to find the directory actually
served. Check that its production file is beside `Sommerhus.Mvc.dll`, has the exact
filename, and sets `Api:BaseUrl` to `https://demo-vac.sima.dk/api/`. Check the pool's
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

Test `https://demo-vac.sima.dk/api/api/cities` from the server. If public-IP loopback/NAT
prevents it reaching itself, use internal DNS or a hosts entry pointing the same hostname to
the server's reachable local address. Keep the hostname in `Api:BaseUrl` so image links
remain usable by browsers.

### Feature image upload fails or the saved image is missing

Feature icons accept **PNG or JPEG, maximum 2 MiB** (2,097,152 bytes). The feature form now
shows this limit and displays API validation errors. Try a small `.png` or `.jpg` first;
other image types and larger files are rejected by the existing API rules.

If a valid file fails with a server error, check the API log for the same request. The API
pool identity needs **Modify** on `<api-path>\wwwroot\uploads` and its children. Grant this
to `IIS AppPool\SommerhusApi`, not the MVC pool. Keep the application binaries read-only;
create the uploads directory during setup as described above.

If the upload succeeds but the image does not display, open its image URL directly. On this
IIS layout it should begin with `https://demo-vac.sima.dk/api/uploads/`; `/api` is the IIS
application prefix, and `/uploads` is the static path. Check the MVC server setting
`Api:BaseUrl` and whether the file exists under the API's `wwwroot\uploads`.

Older releases could start without a physical `wwwroot` in a fresh publish, leaving static
file serving disconnected from the directory created by the first upload. The updated API
initializes its image file provider at startup so newly uploaded files are served immediately.
Redeploy the API fix as well as the MVC form/error-message changes, preserving existing uploads.

### The database is locked or login expires

For a locked database, close tools holding unsaved write transactions. The API token normally
lasts 60 minutes (`Jwt:AccessTokenMinutes`); log in again after expiry.

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

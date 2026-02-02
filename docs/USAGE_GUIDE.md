# Sommerhus Quick Usage Guide

## Getting Started

### Prerequisites
- .NET 8.0 SDK ([download](https://dotnet.microsoft.com/download/dotnet/8.0))
- Git ([download](https://git-scm.com/downloads))
- IDE of choice (VS 2022, Rider, or VS Code)

### First-Time Setup
```powershell
# Clone repository
git clone <repository-url>
cd Sommerhus_project

# Restore packages
dotnet restore Sommerhus_project.sln

# Build solution
dotnet build Sommerhus_project.sln

# Run tests to verify setup
dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj
```

---

## Running the Application

### Start API (Backend)
```powershell
dotnet run --project Sommerhus.Api
```
- **URL**: http://localhost:5183
- **Swagger**: http://localhost:5183/swagger
- **Admin Login**: `admin` / `Sommerhus123!`

### Start MVC (Frontend)
```powershell
dotnet run --project Sommerhus.Mvc
```
- **URL**: http://localhost:5015
- **Admin Panel**: http://localhost:5015/admin

### Run Both with Hot Reload
Open two terminals:
```powershell
# Terminal 1 - API
dotnet watch run --project Sommerhus.Api

# Terminal 2 - MVC
dotnet watch run --project Sommerhus.Mvc
```

---

## Common Tasks

### Run All Tests
```powershell
dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj
```

### Run Specific Test Category
```powershell
# Admin tests only
dotnet test --filter "FullyQualifiedName~Admin"

# Single test class
dotnet test --filter "FullyQualifiedName~HousesTests"
```

### Run Tests with Coverage
```powershell
dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj /p:CollectCoverage=true
```

### Format Code
```powershell
dotnet format Sommerhus_project.sln
```

### Build for Release
```powershell
dotnet build Sommerhus_project.sln -c Release
```

### Publish API
```powershell
dotnet publish Sommerhus.Api -c Release -o ./publish/api
```

---

## Database Operations

### Apply Migrations
```powershell
# Auto-applied on startup, but can run manually:
dotnet ef database update --project Sommerhus.Repository --startup-project Sommerhus.Api
```

### Add New Migration
```powershell
dotnet ef migrations add MigrationName --project Sommerhus.Repository --startup-project Sommerhus.Api
```

### Reset Database (Development)
```powershell
# Delete SQLite file and restart - will recreate with seed data
Remove-Item Sommerhus.Api/sommerhus.db
dotnet run --project Sommerhus.Api
```

### View Current Database
Use "DB Browser for SQLite" to open `Sommerhus.Api/sommerhus.db`

---

## API Testing

### Authenticate via Swagger
1. Open http://localhost:5183/swagger
2. Click "Authorize" button
3. In the "Login" endpoint, POST:
   ```json
   {
     "username": "admin",
     "password": "Sommerhus123!"
   }
   ```
4. Copy the returned token
5. Click "Authorize" and enter: `Bearer <token>`

### Test with PowerShell
```powershell
# Login and get token
$response = Invoke-RestMethod -Uri "http://localhost:5183/admin/auth/login" -Method Post -ContentType "application/json" -Body '{"username":"admin","password":"Sommerhus123!"}'
$token = $response.token

# Make authenticated request
$headers = @{ Authorization = "Bearer $token" }
Invoke-RestMethod -Uri "http://localhost:5183/api/admin/houses" -Headers $headers
```

---

## Git Workflow Guide

### Daily Workflow

#### 1. Start New Work
```powershell
# Ensure you're on main and up to date
git checkout main
git pull origin main

# Create feature branch
git checkout -b feature/your-feature-name
```

#### 2. Make Changes and Commit
```powershell
# Check what's changed
git status

# Stage specific files
git add Sommerhus.Api/Program.cs

# Or stage all changes
git add .

# Commit with descriptive message
git commit -m "Add user authentication endpoint"
```

#### 3. Push Changes
```powershell
# First push (set upstream)
git push -u origin feature/your-feature-name

# Subsequent pushes
git push
```

#### 4. Create Pull Request
- Go to GitHub/GitLab repository
- Click "New Pull Request"
- Select your branch
- Add description of changes
- Request review

### Common Git Commands

| Command | Purpose |
|---------|---------|
| `git status` | See current changes |
| `git diff` | See file differences |
| `git log -5` | View last 5 commits |
| `git branch` | List local branches |
| `git checkout <branch>` | Switch branches |
| `git stash` | Temporarily save changes |
| `git stash pop` | Restore stashed changes |

### Undo Mistakes

```powershell
# Undo staged changes (before commit)
git reset HEAD <file>

# Discard local changes to a file
git checkout -- <file>

# Undo last commit (keep changes)
git reset --soft HEAD~1

# Undo last commit (discard changes) - CAREFUL!
git reset --hard HEAD~1
```

### Sync with Main Branch

```powershell
# While on your feature branch
git fetch origin
git merge origin/main

# Or use rebase (cleaner history)
git rebase origin/main
```

### Resolve Merge Conflicts

1. Open conflicted files (marked with `<<<<<<< HEAD`)
2. Edit to resolve conflicts
3. Remove conflict markers
4. Stage resolved files: `git add <file>`
5. Complete merge: `git commit`

### Branch Naming Convention

| Type | Format | Example |
|------|--------|---------|
| Feature | `feature/short-description` | `feature/add-pricing` |
| Bug Fix | `fix/short-description` | `fix/login-error` |
| Refactor | `refactor/short-description` | `refactor/cleanup-services` |
| Docs | `docs/short-description` | `docs/update-readme` |

### Commit Message Format

```
<type>: <short description>

<optional body explaining what and why>

<optional footer: Closes #123>
```

**Types**: `feat`, `fix`, `refactor`, `docs`, `test`, `chore`

**Examples**:
```
feat: Add house pricing endpoint
fix: Correct image upload path handling
refactor: Extract pricing service from house service
docs: Update API documentation
test: Add integration tests for areas
chore: Update NuGet packages
```

### Before Creating Pull Request

```powershell
# 1. Format code
dotnet format Sommerhus_project.sln

# 2. Build with no warnings
dotnet build Sommerhus_project.sln

# 3. Run all tests
dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj

# 4. Squash WIP commits if needed
git rebase -i origin/main
# Change 'pick' to 'squash' for commits to combine

# 5. Push final changes
git push --force-with-lease  # Only after rebase
```

---

## Troubleshooting

### Build Errors

**"Package not found"**
```powershell
dotnet restore Sommerhus_project.sln
```

**"Target framework not found"**
- Ensure .NET 8 SDK is installed: `dotnet --list-sdks`

### Runtime Errors

**"Connection string not found"**
- Check `appsettings.Development.json` exists
- Verify `ConnectionStrings:Default` is set

**"JWT configuration missing"**
- Check `Jwt` section in appsettings
- Ensure all fields are present: `Issuer`, `Audience`, `Key`, `AccessTokenMinutes`

**"Port already in use"**
```powershell
# Find process using port
netstat -ano | findstr :5183
# Kill process
taskkill /PID <process-id> /F
```

### Test Failures

**"Database locked"**
- Close any SQLite database viewers
- Restart test runner

**"Connection refused"**
- Ensure API is running if testing MVC
- Check ports in launchSettings.json

---

## IDE Tips

### Visual Studio 2022
- **Run Multiple Projects**: Right-click solution → Set Startup Projects → Multiple
- **Hot Reload**: Edit code while debugging
- **Test Explorer**: View → Test Explorer

### VS Code
- **Tasks**: `Ctrl+Shift+P` → "Run Task" → Build/Test
- **Debug**: F5 with `.vscode/launch.json` configured
- **Extensions**: C# Dev Kit, GitLens, REST Client

### JetBrains Rider
- **Run Configuration**: Edit Configurations → Compound for API+MVC
- **Database Tools**: View → Tool Windows → Database
- **Unit Tests**: Right-click test file → Run

---

## Quick Reference Card

| Task | Command |
|------|---------|
| Build | `dotnet build Sommerhus_project.sln` |
| Run API | `dotnet run --project Sommerhus.Api` |
| Run MVC | `dotnet run --project Sommerhus.Mvc` |
| Test | `dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj` |
| Format | `dotnet format Sommerhus_project.sln` |
| New branch | `git checkout -b feature/name` |
| Commit | `git add . && git commit -m "message"` |
| Push | `git push` |
| Pull latest | `git pull origin main` |

# Repository Guidelines

## Project Structure & Module Organization
- Sommerhus.Api/: ASP.NET Core backend with controllers in Controllers/, EF helpers in Services/, and the seeded SQLite catalog sommerhus.db.
- Sommerhus.Mvc/: Razor frontend; shared chrome in Views/Shared, admin pages in Views/Admin, static assets inside wwwroot/ (images/, css/, js/).
- Sommerhus.Api.Tests/: xUnit suites organised by feature (Admin, Public, Infrastructure) plus reusable fixtures for database bootstrapping.
- Solution orchestration lives in Sommerhus_project.sln; environment overrides belong in the relevant appsettings*.json file.

## Build, Test, and Development Commands
- dotnet restore hydrates packages after cloning or changing SDK versions.
- dotnet build Sommerhus_project.sln compiles API, MVC, and tests in one pass.
- dotnet watch run --project Sommerhus.Mvc serves the UI on http://localhost:5015; run the API with the same command targeting Sommerhus.Api (Swagger on http://localhost:5183).
- dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj /p:CollectCoverage=true runs all suites and emits Coverlet coverage.

## Coding Style & Naming Conventions
- Default to four-space indentation, file-scoped namespaces, and PascalCase public members with camelCase locals.
- Suffix async methods with Async, prefer expression-bodied members for short accessors, and keep controllers thin by delegating to services.
- Reuse DTOs or view models before adding new ones; share UI-specific helpers through Sommerhus.Mvc/Services.
- Run dotnet format (global tool) when touching multiple files and resolve nullable warnings before committing.

## Testing Guidelines
- Place new specs beside the related folder and name them <Feature>Tests (e.g., Admin/HouseImagesTests.cs).
- Use the provided SQLite fixture utilities to isolate test data and prevent cross-test leakage.
- Rely on FluentAssertions for readable expectations; document any required manual verification steps in the PR description.

## Commit & Pull Request Guidelines
- Write concise, imperative commit subjects (Fix admin image upload) and reference issues with Fixes #123 when applicable.
- Squash WIP commits before review and note schema or configuration changes in the commit body or PR.
- PRs should summarise the user impact, list new environment variables or migrations, and include before/after screenshots for notable UI work.
- Always share the dotnet test command you executed so reviewers can trace coverage results.

## Configuration & Data Tips
- Keep Sommerhus.Api/sommerhus.db for local data only; ship EF migrations or seed scripts for real dataset updates.
- Store secrets in user secrets or environment variables referenced from appsettings.Development.json.
- Optimise new images (<500 KB) before adding them to wwwroot/images and name files after the house or area slug for clarity.

# Contributing to Sommerhus

## Development Workflow

### 1. Pick a Task
- Check `docs/ROADMAP.md` for prioritized work
- Review `docs/KNOWN_ISSUES.md` for bugs/debt
- Discuss larger changes before starting

### 2. Create Branch
```powershell
git checkout main
git pull origin main
git checkout -b feature/your-task-name
```

### 3. Make Changes
- Follow conventions in `.windsurfrules`
- Keep changes focused and minimal
- Add/update tests for logic changes

### 4. Verify Changes
```powershell
dotnet format Sommerhus_project.sln
dotnet build Sommerhus_project.sln
dotnet test Sommerhus.Api.Tests/Sommerhus.Api.Tests.csproj
```

### 5. Commit & Push
```powershell
git add .
git commit -m "feat: Description of change"
git push -u origin feature/your-task-name
```

### 6. Create Pull Request
- Summarize what changed and why
- Link related issues
- Request review

---

## Code Style Quick Reference

| Aspect | Convention |
|--------|------------|
| Indentation | 4 spaces |
| Namespaces | File-scoped (`namespace Foo;`) |
| Async methods | Suffix with `Async`, accept `CancellationToken` |
| Interfaces | Prefix with `I` |
| DTOs | Suffix with `Dto`, use `record` |
| Private fields | camelCase |
| Public members | PascalCase |

---

## Adding New Features

### Backend (API)
1. Define interface in `Sommerhus.Application/{Admin|Public}/`
2. Create implementation in `Sommerhus.Repository/{Admin|Public}/`
3. Add controller in `Sommerhus.Api/Controllers/{Admin|Public}/`
4. Register service in Program.cs or extension method
5. Add integration tests

### Frontend (MVC)
1. Add method to appropriate client in `Sommerhus.Mvc/Services/`
2. Create controller action
3. Create view
4. Test manually

### DTOs
1. Add to `Sommerhus.Contracts/Dtos/{Admin|Public|Shared}/`
2. Include validation attributes
3. Use `record` for immutable types

---

## Testing Guidelines

### Naming Convention
```
{Method}_{Scenario}_{ExpectedResult}
```

Example: `CreateHouse_WithValidData_ReturnsCreatedId`

### Test Structure
```csharp
[Fact]
public async Task CreateHouse_WithValidData_ReturnsCreatedId()
{
    // Arrange
    await using var factory = new CustomWebApplicationFactory();
    using var client = factory.CreateClient();
    var dto = new UpsertHouseDto(...);

    // Act
    var response = await client.PostAsJsonAsync("api/admin/houses", dto);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var id = await response.Content.ReadFromJsonAsync<Guid>();
    id.Should().NotBeEmpty();
}
```

---

## Commit Message Format

```
<type>: <short description>
```

**Types**:
- `feat` - New feature
- `fix` - Bug fix
- `refactor` - Code refactoring
- `docs` - Documentation
- `test` - Tests
- `chore` - Maintenance

**Examples**:
```
feat: Add house pricing endpoint
fix: Correct image path generation
refactor: Extract pricing service
docs: Update architecture documentation
test: Add authorization tests
chore: Update EF Core to 8.0.11
```

---

## Pull Request Checklist

Before requesting review:

- [ ] Code follows project conventions
- [ ] `dotnet format` has been run
- [ ] All tests pass
- [ ] No build warnings
- [ ] Changes are documented (if API changes)
- [ ] Commit history is clean (WIP commits squashed)

---

## Questions?

- Check existing documentation in `docs/`
- Review `.windsurfrules` for conventions
- Look at similar existing code for patterns

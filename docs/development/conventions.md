# WorkGrid — Development Conventions & Standards

## 1. Language & Framework Baseline

* **Target Framework:** .NET 8 LTS (`net8.0`, `net8.0-android`)
* **C# Version:** Latest (`<LangVersion>latest</LangVersion>`)
* **Nullable Reference Types:** Enabled project-wide (`<Nullable>enable</Nullable>`)
* **Implicit Usings:** Enabled project-wide (`<ImplicitUsings>enable</ImplicitUsings>`)

---

## 2. Naming & Code Style Conventions

### Namespaces & Assemblies
* Root namespace format: `WorkGrid.<Layer>.<Feature>` (e.g., `WorkGrid.Domain.Entities`, `WorkGrid.App.ViewModels`)

### Naming Guidelines
| Element | Convention | Example |
|---|---|---|
| Classes, Records, Structs | `PascalCase` | `Employee`, `AssetRepository` |
| Interfaces | `IPascalCase` | `IAssetRepository`, `IDateTimeProvider` |
| Methods & Properties | `PascalCase` | `GetActiveEmployeesAsync()`, `CreatedAtUtc` |
| Method Parameters | `camelCase` | `employeeId`, `cancellationToken` |
| Private Fields | `_camelCase` | `_logger`, `_databaseContext` |
| Constants | `PascalCase` | `MaxDescriptionLength` |
| Enums & Enum Values | `PascalCase` | `AssetStatus.Available`, `AssetStatus.InMaintenance` |

---

## 3. Git Workflow & Commit Guidelines

We adhere to the [Conventional Commits](https://www.conventionalcommits.org/) standard.

### Commit Types
* `feat:` A new feature or product capability
* `fix:` A bug fix
* `docs:` Documentation changes only
* `chore:` Scaffolding, tooling, package updates, build scripts
* `refactor:` Code restructuring without functional change
* `test:` Adding or updating tests

### Commit Message Format
```text
<type>(<scope>): <short summary>

[optional body explaining motivation and consequences]
Example:

chore(solution): establish project references and central build props
```

## 4. Pre-Commit Verification Checklist

>Before submitting changes or committing:

 1. Solution builds cleanly with zero errors: dotnet build
 2. All test suites pass without failure: dotnet test
 3. No unnecessary or temporary files are untracked
 4. No secrets, keys, or machine-specific paths committed

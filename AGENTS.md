# Steeltoe Build and Test Guide for Developers and CI/CD Agents

This document provides essential guidelines for working with the Steeltoe codebase. For detailed build and test procedures, refer to the [GitHub workflows](.github/workflows/) which serve as the source of truth.

## General Guidelines

### Code Review and Suggestions
- Only make high confidence suggestions when reviewing code changes.
- Always use the latest stable version of C#.
- Never add or change `global.json` unless explicitly asked to.
- Never change `NuGet.config` files unless explicitly asked to.

### Null Handling
- Declare variables non-nullable, and check for null at public API entry points.
- Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.

### Writing Tests
- Do not emit "Act", "Arrange" or "Assert" comments in test code.
- Tests should pass before committing and pushing changes.
- When possible, code coverage levels should be increased or at least maintained.

## Prerequisites

- **.NET SDK 10.0** (latest patch version)
- **.NET Runtime 8.0** (latest patch version)
- **.NET Runtime 9.0** (latest patch version)

Verify your installation:
```bash
dotnet --list-sdks
dotnet --list-runtimes
```

## Quick Start

### Build the Solution

```bash
dotnet restore src/Steeltoe.All.sln /p:Configuration=Release --verbosity minimal
dotnet build src/Steeltoe.All.sln --no-restore --configuration Release --verbosity minimal
```

### Run Tests

For detailed test procedures including environment-specific filters, test categories, and coverage collection, see [`.github/workflows/Steeltoe.All.yml`](.github/workflows/Steeltoe.All.yml).

Quick test command:
```bash
dotnet test src/Steeltoe.All.sln --framework net10.0 --no-build --configuration Release
```

**Important context for agents:**
- Tests run on multiple frameworks: net8.0, net9.0, and net10.0
- Tests use xUnit trait categories: `Integration` (requires Docker services), `MemoryDumps` (generates memory dumps), and `SkipOnMacOS` (platform-specific)
- Integration tests require Docker and are primarily designed for Linux CI environments
- When writing tests, use appropriate categories: `[Trait("Category", "Integration")]` for tests requiring external services

## Code Style Validation

Steeltoe uses ReSharper/Rider code cleanup tools via `regitlint` to enforce consistent code style. The CI workflow (`.github/workflows/verify-code-style.yml`) automatically verifies code style on all pull requests.

To run code cleanup locally:
```powershell
./cleanupcode.ps1
```

If your PR fails the code style check, run `cleanupcode.ps1` locally and commit the changes.

## Additional Resources

- [Steeltoe Documentation](https://steeltoe.io/)
- [Contributing Guidelines](https://github.com/SteeltoeOSS/Steeltoe/wiki)
- [CI Workflow](.github/workflows/Steeltoe.All.yml)
- [Code Style Workflow](.github/workflows/verify-code-style.yml)

# Steeltoe Build and Test Guide for Developers and CI/CD Agents

This document provides comprehensive instructions for building and testing the Steeltoe codebase. These instructions are based on the official CI workflow and are designed to work for both human developers and automated CI/CD agents.

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

## Prerequisites

Before building and testing Steeltoe, ensure you have the following installed:

- **.NET SDK 10.0** (latest patch version)
- **.NET Runtime 8.0** (latest patch version)
- **.NET Runtime 9.0** (latest patch version)

You can download the .NET SDK and runtimes from:
- [.NET 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- [.NET 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)
- [.NET 10.0](https://dotnet.microsoft.com/download/dotnet/10.0)

Verify your installation:
```bash
dotnet --list-sdks
dotnet --list-runtimes
```

## Quick Start

### Build the Solution

```bash
# Restore NuGet packages
dotnet restore src/Steeltoe.All.sln /p:Configuration=Release --verbosity minimal

# Build the solution
dotnet build src/Steeltoe.All.sln --no-restore --configuration Release --verbosity minimal
```

### Run All Tests

**Important:** Tests should pass before committing and pushing changes. When possible, code coverage levels should be increased or at least maintained.

```bash
# Test with .NET 10.0
dotnet test src/Steeltoe.All.sln --framework net10.0 --no-build --configuration Release

# Test with .NET 9.0
dotnet test src/Steeltoe.All.sln --framework net9.0 --no-build --configuration Release

# Test with .NET 8.0
dotnet test src/Steeltoe.All.sln --framework net8.0 --no-build --configuration Release
```

## Detailed Test Instructions

### Environment-Specific Test Filters

Different operating systems have different capabilities for running integration tests. Use the appropriate filters for your environment:

#### Linux (Ubuntu)
```bash
# Run all tests (including integration tests)
dotnet test src/Steeltoe.All.sln --framework net10.0 --no-build --configuration Release
```

#### Windows
```bash
# Skip integration tests
dotnet test src/Steeltoe.All.sln --framework net10.0 --no-build --configuration Release --filter "Category!=Integration"
```

#### macOS
```bash
# Skip integration tests and macOS-specific skipped tests
dotnet test src/Steeltoe.All.sln --framework net10.0 --no-build --configuration Release --filter "Category!=Integration&Category!=SkipOnMacOS"
```

### Running Tests with Coverage

The CI workflow includes code coverage collection. To run tests with coverage locally:

```bash
dotnet test src/Steeltoe.All.sln --framework net10.0 \
  --no-build --configuration Release \
  --collect "XPlat Code Coverage" \
  --logger trx \
  --results-directory ./dumps \
  --settings coverlet.runsettings \
  --blame-crash \
  --blame-hang-timeout 3m
```

### Memory Dump Tests

Some tests are designed to generate memory dumps and are separated from regular tests:

```bash
# Run regular tests (excluding memory dump tests)
dotnet test src/Steeltoe.All.sln --framework net10.0 \
  --no-build --configuration Release \
  --filter "Category!=MemoryDumps"

# Run memory dump tests separately
dotnet test src/Steeltoe.All.sln --framework net10.0 \
  --no-build --configuration Release \
  --filter "Category=MemoryDumps"
```

## Docker Services for Integration Tests

Integration tests on Linux require Docker services to be running. The CI workflow uses the following services:

### Eureka Server
```bash
docker run -d -p 8761:8761 --name eurekaServer steeltoe.azurecr.io/eureka-server
```

### Config Server
```bash
# Note: When running locally, you may need to adjust the network configuration
# to allow the Config Server to communicate with Eureka Server.
# The CI workflow uses Docker services with automatic networking.

docker run -d -p 8888:8888 \
  --name configServer \
  --link eurekaServer \
  -e eureka.client.enabled=true \
  -e eureka.client.serviceUrl.defaultZone=http://eurekaServer:8761/eureka \
  -e eureka.instance.hostname=localhost \
  -e eureka.instance.instanceId=localhost:configServer:8888 \
  steeltoe.azurecr.io/config-server
```

**Note:** These Docker images are hosted in the Steeltoe Azure Container Registry, which allows anonymous pulls. No authentication is required to pull these images.

## Complete Build and Test Script

Here's a complete script that mirrors the CI workflow:

```bash
#!/bin/bash

# Configuration
SOLUTION_FILE="src/Steeltoe.All.sln"
CONFIGURATION="Release"

# Restore packages
echo "Restoring packages..."
dotnet restore $SOLUTION_FILE /p:Configuration=$CONFIGURATION --verbosity minimal

# Build solution
echo "Building solution..."
dotnet build $SOLUTION_FILE --no-restore --configuration $CONFIGURATION --verbosity minimal

# Determine OS-specific test filter
if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    SKIP_FILTER="Category!=MemoryDumps"
elif [[ "$OSTYPE" == "darwin"* ]]; then
    SKIP_FILTER="Category!=Integration&Category!=SkipOnMacOS&Category!=MemoryDumps"
elif [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" ]]; then
    SKIP_FILTER="Category!=Integration&Category!=MemoryDumps"
else
    SKIP_FILTER="Category!=MemoryDumps"
fi

echo "Using test filter: $SKIP_FILTER"

# Test with .NET 10.0
echo "Testing with .NET 10.0..."
dotnet test $SOLUTION_FILE --framework net10.0 --filter "$SKIP_FILTER" \
  --no-build --configuration $CONFIGURATION \
  --collect "XPlat Code Coverage" \
  --logger trx \
  --results-directory ./dumps \
  --settings coverlet.runsettings \
  --blame-crash \
  --blame-hang-timeout 3m

# Test with .NET 9.0
echo "Testing with .NET 9.0..."
dotnet test $SOLUTION_FILE --framework net9.0 --filter "$SKIP_FILTER" \
  --no-build --configuration $CONFIGURATION \
  --collect "XPlat Code Coverage" \
  --logger trx \
  --results-directory ./dumps \
  --settings coverlet.runsettings \
  --blame-crash \
  --blame-hang-timeout 3m

# Test with .NET 8.0
echo "Testing with .NET 8.0..."
dotnet test $SOLUTION_FILE --framework net8.0 --filter "$SKIP_FILTER" \
  --no-build --configuration $CONFIGURATION \
  --collect "XPlat Code Coverage" \
  --logger trx \
  --results-directory ./dumps \
  --settings coverlet.runsettings \
  --blame-crash \
  --blame-hang-timeout 3m

echo "Build and test completed!"
```

## Code Style Validation

Steeltoe uses ReSharper/Rider code cleanup tools via `regitlint` to enforce consistent code style. Before submitting a PR, ensure your code passes style validation.

### Using the cleanupcode.ps1 Script

The repository includes a PowerShell script to run code cleanup:

```powershell
# Run cleanup on all files
./cleanupcode.ps1

# Run cleanup on files changed since a specific branch/commit
./cleanupcode.ps1 -revision main
```

**Note:** This script requires PowerShell 7.0 or later.

### Manual Code Style Check

You can also run the code style check manually:

```bash
# Restore .NET tools
dotnet tool restore

# Restore packages
dotnet restore src/Steeltoe.All.sln /p:Configuration=Release --verbosity minimal

# Run code cleanup check (without making changes)
dotnet regitlint -s src/Steeltoe.All.sln --print-command --skip-tool-check \
  --jb --dotnetcoresdk=$(dotnet --version) \
  --jb-profile="Steeltoe Full Cleanup" \
  --jb --properties:Configuration=Release \
  --jb --properties:NuGetAudit=false \
  --jb --verbosity=WARN \
  --fail-on-diff --print-diff
```

### CI Code Style Verification

The CI workflow (`.github/workflows/verify-code-style.yml`) automatically verifies code style on all pull requests. If your PR fails the code style check, run `cleanupcode.ps1` locally and commit the changes.

## Troubleshooting

### Common Issues

#### Missing .NET SDK
```
Error: The specified framework 'Microsoft.NETCore.App', version 'X.X.X' was not found.
```
**Solution:** Install the required .NET SDK version (8.0, 9.0, or 10.0).

#### NuGet Restore Failures
```
Error: Unable to load the service index for source...
```
**Solution:** Check your network connection and ensure you have access to NuGet.org and the Steeltoe development feed if using pre-release packages.

#### Integration Tests Failing on Windows/macOS
**Solution:** Integration tests require Docker services and are designed to run primarily on Linux. Use the appropriate test filters shown above.

#### Test Timeouts
**Solution:** The `--blame-hang-timeout 3m` flag helps identify hanging tests. If tests consistently timeout, there may be an environmental issue or resource constraint.

#### Build Warnings During Refactoring
If temporarily introducing warnings during refactoring, add `/p:TreatWarningsAsErrors=false` to prevent build failure:
```bash
dotnet build src/Steeltoe.All.sln --configuration Release /p:TreatWarningsAsErrors=false
```
**Important:** All warnings should be addressed before committing any final changes.

### Docker Authentication Issues
If you need to authenticate with the Steeltoe Azure Container Registry for integration tests:
```bash
az acr login --name steeltoe
```

### Sandboxed Environment Limitations
When running in sandboxed or restricted network environments:
- Some Azure DevOps NuGet feeds may be blocked
- Docker registry access may be restricted
- Integration tests may not be feasible

In such cases, focus on:
- Building the solution successfully
- Running unit tests (with `Category!=Integration` filter)
- Verifying code changes compile without errors

## Additional Resources

- [Steeltoe Documentation](https://steeltoe.io/)
- [Contributing Guidelines](https://github.com/SteeltoeOSS/Steeltoe/wiki)
- [CI Workflow](.github/workflows/Steeltoe.All.yml)
- [Issue Tracker](https://github.com/SteeltoeOSS/Steeltoe/issues)

## Environment Variables

The CI workflow uses the following environment variables:
- `DOTNET_CLI_TELEMETRY_OPTOUT=1` - Disable .NET CLI telemetry
- `DOTNET_NOLOGO=true` - Suppress .NET logo output
- `DOTNET_GENERATE_ASPNET_CERTIFICATE=false` (macOS only) - Prevent certificate prompts

You can set these in your shell for a cleaner build experience:
```bash
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=true
```

## Test Categories

Tests are organized using xUnit trait categories:
- **Integration** - Tests requiring external services (Docker containers)
- **MemoryDumps** - Tests that generate memory dumps for analysis
- **SkipOnMacOS** - Tests that cannot run on macOS due to platform limitations

Use these categories with the `--filter` parameter to control which tests run.

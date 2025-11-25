# Steeltoe Build and Test Instructions for Agents

This document provides step-by-step instructions for building and testing the Steeltoe codebase, based on the commands used in the official CI/CD workflow.

## Prerequisites

### .NET SDK Requirements
You need both .NET 8.0 and .NET 10.0 SDKs installed. The project targets multiple frameworks:
- .NET 8.0 (net8.0)
- .NET 10.0 (net10.0)

Install both SDKs:
```bash
# Install .NET 8.0 and 10.0 SDKs
# Follow instructions at: https://dotnet.microsoft.com/download
```

### Environment Variables
Set the following environment variables for optimal build experience:
```bash
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=true
```

## Build Instructions

### 1. Navigate to Repository Root
```bash
cd /path/to/Steeltoe
```

### 2. Restore NuGet Packages
```bash
dotnet restore src/Steeltoe.All.sln /p:Configuration=Release --verbosity minimal
```

**Note:** If you encounter network connectivity issues with Azure DevOps feeds (like `frdvsblobprodcus327.vsblob.vsassets.io`), this is a known limitation in sandboxed environments. The restore will still succeed for most packages.

### 3. Build the Solution
```bash
dotnet build src/Steeltoe.All.sln --no-restore --configuration Release --verbosity minimal
```

## Test Instructions

The project includes comprehensive test suites for multiple target frameworks. Tests are categorized and can be filtered based on the environment.

### Environment-Specific Test Filters

#### Linux (with Docker containers)
```bash
# Set test filter for all tests except memory dumps
export SKIP_FILTER_NO_MEMORY_DUMPS="Category!=MemoryDumps"
export SKIP_FILTER_WITH_MEMORY_DUMPS="Category=MemoryDumps"
```

#### Windows
```bash
# Skip integration tests on Windows
export SKIP_FILTER_NO_MEMORY_DUMPS="Category!=Integration&Category!=MemoryDumps"
export SKIP_FILTER_WITH_MEMORY_DUMPS="Category!=Integration&Category=MemoryDumps"
```

#### macOS
```bash
# Skip integration tests and macOS-specific tests
export SKIP_FILTER_NO_MEMORY_DUMPS="Category!=Integration&Category!=SkipOnMacOS&Category!=MemoryDumps"
export SKIP_FILTER_WITH_MEMORY_DUMPS="Category!=Integration&Category!=SkipOnMacOS&Category=MemoryDumps"

# Prevent dev certificate prompt on macOS
export DOTNET_GENERATE_ASPNET_CERTIFICATE=false
```

### Running Tests

#### Common Test Arguments
```bash
export COMMON_TEST_ARGS="--no-build --configuration Release --collect \"XPlat Code Coverage\" --logger trx --results-directory ./dumps --settings coverlet.runsettings --blame-crash --blame-hang-timeout 3m"
```

#### Test .NET 8.0 Framework
```bash
# Regular tests (excluding memory dumps)
dotnet test src/Steeltoe.All.sln --framework net8.0 --filter "$SKIP_FILTER_NO_MEMORY_DUMPS" $COMMON_TEST_ARGS

# Memory dump tests
dotnet test src/Steeltoe.All.sln --framework net8.0 --filter "$SKIP_FILTER_WITH_MEMORY_DUMPS" $COMMON_TEST_ARGS
```

#### Test .NET 10.0 Framework
```bash
# Regular tests (excluding memory dumps)
dotnet test src/Steeltoe.All.sln --framework net10.0 --filter "$SKIP_FILTER_NO_MEMORY_DUMPS" $COMMON_TEST_ARGS

# Memory dump tests
dotnet test src/Steeltoe.All.sln --framework net10.0 --filter "$SKIP_FILTER_WITH_MEMORY_DUMPS" $COMMON_TEST_ARGS
```

## Quick Build and Test Script

For convenience, here's a complete script to build and test:

```bash
#!/bin/bash

# Set environment variables
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=true

# Set OS-specific filters
if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS
    export DOTNET_GENERATE_ASPNET_CERTIFICATE=false
    export SKIP_FILTER_NO_MEMORY_DUMPS="Category!=Integration&Category!=SkipOnMacOS&Category!=MemoryDumps"
    export SKIP_FILTER_WITH_MEMORY_DUMPS="Category!=Integration&Category!=SkipOnMacOS&Category=MemoryDumps"
elif [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "win32" ]]; then
    # Windows
    export SKIP_FILTER_NO_MEMORY_DUMPS="Category!=Integration&Category!=MemoryDumps"
    export SKIP_FILTER_WITH_MEMORY_DUMPS="Category!=Integration&Category=MemoryDumps"
else
    # Linux
    export SKIP_FILTER_NO_MEMORY_DUMPS="Category!=MemoryDumps"
    export SKIP_FILTER_WITH_MEMORY_DUMPS="Category=MemoryDumps"
fi

# Common test arguments
export COMMON_TEST_ARGS="--no-build --configuration Release --collect \"XPlat Code Coverage\" --logger trx --results-directory ./dumps --settings coverlet.runsettings --blame-crash --blame-hang-timeout 3m"

echo "Building Steeltoe..."

# Restore packages
dotnet restore src/Steeltoe.All.sln /p:Configuration=Release --verbosity minimal

# Build solution
dotnet build src/Steeltoe.All.sln --no-restore --configuration Release --verbosity minimal

echo "Running tests..."

# Test .NET 8.0
dotnet test src/Steeltoe.All.sln --framework net8.0 --filter "$SKIP_FILTER_NO_MEMORY_DUMPS" $COMMON_TEST_ARGS
dotnet test src/Steeltoe.All.sln --framework net8.0 --filter "$SKIP_FILTER_WITH_MEMORY_DUMPS" $COMMON_TEST_ARGS

# Test .NET 10.0 (if SDK is available)
if dotnet --list-sdks | grep -q "10\."; then
    dotnet test src/Steeltoe.All.sln --framework net10.0 --filter "$SKIP_FILTER_NO_MEMORY_DUMPS" $COMMON_TEST_ARGS
    dotnet test src/Steeltoe.All.sln --framework net10.0 --filter "$SKIP_FILTER_WITH_MEMORY_DUMPS" $COMMON_TEST_ARGS
else
    echo "Warning: .NET 10.0 SDK not found, skipping .NET 10.0 tests"
fi

echo "Build and test completed!"
```

## Docker Services (Linux only)

For integration tests on Linux, the following Docker services should be running:

```bash
# Eureka Server
docker run -d --name eureka-server -p 8761:8761 steeltoe.azurecr.io/eureka-server

# Config Server
docker run -d --name config-server -p 8888:8888 \
  -e "eureka.client.enabled=true" \
  -e "eureka.client.serviceUrl.defaultZone=http://localhost:8761/eureka" \
  -e "eureka.instance.hostname=localhost" \
  -e "eureka.instance.instanceId=localhost:configServer:8888" \
  steeltoe.azurecr.io/config-server
```

## Troubleshooting

### Common Issues

1. **Network connectivity errors**: Some Azure DevOps package feeds (especially `frdvsblobprodcus327.vsblob.vsassets.io`) may be blocked in sandboxed environments. This affects packages like `System.CommandLine.2.0.0-beta4.24324.3`. This is a known limitation in sandboxed environments and is expected.

2. **.NET 10.0 not found**: Install the .NET 10.0 SDK or skip .NET 10.0 tests by only running .NET 8.0 tests. You'll see errors like `NETSDK1045: The current .NET SDK does not support targeting .NET 10.0`.

3. **macOS certificate prompts**: Set `DOTNET_GENERATE_ASPNET_CERTIFICATE=false` and use the `SkipOnMacOS` test filter.

4. **Long test execution**: Tests have a 3-minute timeout for blame-hang detection. Some tests may take a while to complete.

### Sandboxed Environment Limitations

When working in sandboxed environments (like GitHub Copilot agents), you may encounter:

- **Azure DevOps feed access blocked**: Cannot download certain packages from `pkgs.dev.azure.com`
- **Limited .NET SDK versions**: May not have access to install additional .NET SDK versions
- **Network restrictions**: Some external package sources may be inaccessible

These limitations don't prevent you from working with the codebase but may affect the ability to restore all packages or run the complete test suite.

### Alternative Solution Files

If you need to work with specific components, you can use the filtered solution files:
- `src/Steeltoe.Common.slnf` - Common utilities
- `src/Steeltoe.Configuration.slnf` - Configuration providers
- `src/Steeltoe.Connectors.slnf` - Service connectors
- `src/Steeltoe.Discovery.slnf` - Service discovery
- `src/Steeltoe.Logging.slnf` - Logging providers
- `src/Steeltoe.Management.slnf` - Management endpoints
- `src/Steeltoe.Security.slnf` - Security providers

Replace `src/Steeltoe.All.sln` with any of these files in the commands above to work with specific components.

## Code Coverage

Test runs generate code coverage reports in the `coveragereport` directory. The reports exclude generated files (`*.g.cs`) and provide both summary and detailed coverage information.
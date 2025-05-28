// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.Health;
using Steeltoe.Management.Endpoint.Actuators.Health.Contributors;
using Steeltoe.Management.Endpoint.Actuators.Health.Contributors.FileSystem;
using Steeltoe.Management.Endpoint.Test.Actuators.Health.Contributors.FileSystem;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health.Contributors;

public sealed class DiskSpaceHealthContributorTest
{
    private const long OneMegabyte = 1024 * 1024;
    private const long DiskFreeSpace = 32 * OneMegabyte;
    private const long DiskTotalSpace = 128 * OneMegabyte;
    private const string NetworkPath = @"\\SERVER\share\team\data";
    private static readonly List<FakeNetworkShareWrapper> NetworkShares = [new(@"\\server\share", DiskFreeSpace, DiskTotalSpace)];
    private static readonly string PlatformPath = Platform.IsWindows ? @"d:\Apps\Data" : "/mnt/apps/data";
    private static readonly List<FakeDriveInfoWrapper> PlatformDrives = [new(DiskFreeSpace, DiskTotalSpace, Platform.IsWindows ? @"D:\" : "/")];

    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Management:Endpoints:Actuator:Exposure:Include:0"] = "health",
        ["Management:Endpoints:Health:ShowComponents"] = "Always",
        ["Management:Endpoints:Health:ShowDetails"] = "Always"
    };

    [Fact]
    public async Task Configures_default_settings()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Services.AddHealthActuator();
        await using WebApplication host = builder.Build();

        DiskSpaceContributorOptions options = host.Services.GetRequiredService<IOptions<DiskSpaceContributorOptions>>().Value;

        options.Threshold.Should().Be(10 * OneMegabyte);
        options.Path.Should().Be(".");
        options.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task Configures_custom_settings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Health:DiskSpace:Threshold"] = $"{25 * OneMegabyte}",
            ["Management:Endpoints:Health:DiskSpace:Path"] = "/mnt/shared/data",
            ["Management:Endpoints:Health:DiskSpace:Enabled"] = "false"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddHealthActuator();
        await using WebApplication host = builder.Build();

        DiskSpaceContributorOptions options = host.Services.GetRequiredService<IOptions<DiskSpaceContributorOptions>>().Value;

        options.Threshold.Should().Be(25 * OneMegabyte);
        options.Path.Should().Be("/mnt/shared/data");
        options.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task Reports_success_when_sufficient_local_free_space()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:Health:DiskSpace:Threshold"] = $"{5 * OneMegabyte}",
            ["Management:Endpoints:Health:DiskSpace:Path"] = PlatformPath
        };

        var diskSpaceProvider = new FakeDiskSpaceProvider(Platform.IsWindows, PlatformDrives, NetworkShares, [PlatformPath]);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<IDiskSpaceProvider>(diskSpaceProvider);
        builder.Services.AddHealthActuator();
        builder.Services.RemoveAll<IHealthContributor>();
        builder.Services.AddSingleton<IHealthContributor, DiskSpaceHealthContributor>();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson($$"""
            {
              "status": "UP",
              "components": {
                "diskSpace": {
                  "status": "UP",
                  "details": {
                    "total": {{DiskTotalSpace}},
                    "free": {{DiskFreeSpace}},
                    "threshold": {{5 * OneMegabyte}},
                    "path": {{JsonValue.Create(PlatformPath).ToJsonString()}},
                    "exists": true
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Reports_failure_when_insufficient_local_free_space()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:Health:DiskSpace:Threshold"] = $"{512 * OneMegabyte}",
            ["Management:Endpoints:Health:DiskSpace:Path"] = PlatformPath
        };

        var diskSpaceProvider = new FakeDiskSpaceProvider(Platform.IsWindows, PlatformDrives, NetworkShares, [PlatformPath]);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<IDiskSpaceProvider>(diskSpaceProvider);
        builder.Services.AddHealthActuator();
        builder.Services.RemoveAll<IHealthContributor>();
        builder.Services.AddSingleton<IHealthContributor, DiskSpaceHealthContributor>();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson($$"""
            {
              "status": "DOWN",
              "components": {
                "diskSpace": {
                  "status": "DOWN",
                  "details": {
                    "total": {{DiskTotalSpace}},
                    "free": {{DiskFreeSpace}},
                    "threshold": {{512 * OneMegabyte}},
                    "path": {{JsonValue.Create(PlatformPath).ToJsonString()}},
                    "exists": true
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Reports_unknown_when_local_directory_does_not_exist()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:Health:DiskSpace:Path"] = PlatformPath
        };

        var diskSpaceProvider = new FakeDiskSpaceProvider(Platform.IsWindows, PlatformDrives, NetworkShares, []);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<IDiskSpaceProvider>(diskSpaceProvider);
        builder.Services.AddHealthActuator();
        builder.Services.RemoveAll<IHealthContributor>();
        builder.Services.AddSingleton<IHealthContributor, DiskSpaceHealthContributor>();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson($$"""
            {
              "status": "UNKNOWN",
              "components": {
                "diskSpace": {
                  "status": "UNKNOWN",
                  "description": "Failed to determine free disk space.",
                  "details": {
                    "error": "The configured path is invalid or does not exist.",
                    "path": {{JsonValue.Create(PlatformPath).ToJsonString()}}
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Reports_success_when_sufficient_network_free_space()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:Health:DiskSpace:Threshold"] = $"{5 * OneMegabyte}",
            ["Management:Endpoints:Health:DiskSpace:Path"] = NetworkPath
        };

        var diskSpaceProvider = new FakeDiskSpaceProvider(true, PlatformDrives, NetworkShares, [NetworkPath]);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<IDiskSpaceProvider>(diskSpaceProvider);
        builder.Services.AddHealthActuator();
        builder.Services.RemoveAll<IHealthContributor>();
        builder.Services.AddSingleton<IHealthContributor, DiskSpaceHealthContributor>();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson($$"""
            {
              "status": "UP",
              "components": {
                "diskSpace": {
                  "status": "UP",
                  "details": {
                    "total": {{DiskTotalSpace}},
                    "free": {{DiskFreeSpace}},
                    "threshold": {{5 * OneMegabyte}},
                    "path": {{JsonValue.Create(NetworkPath).ToJsonString()}},
                    "exists": true
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Reports_failure_when_insufficient_network_free_space()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:Health:DiskSpace:Threshold"] = $"{512 * OneMegabyte}",
            ["Management:Endpoints:Health:DiskSpace:Path"] = NetworkPath
        };

        var diskSpaceProvider = new FakeDiskSpaceProvider(true, PlatformDrives, NetworkShares, [NetworkPath]);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<IDiskSpaceProvider>(diskSpaceProvider);
        builder.Services.AddHealthActuator();
        builder.Services.RemoveAll<IHealthContributor>();
        builder.Services.AddSingleton<IHealthContributor, DiskSpaceHealthContributor>();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson($$"""
            {
              "status": "DOWN",
              "components": {
                "diskSpace": {
                  "status": "DOWN",
                  "details": {
                    "total": {{DiskTotalSpace}},
                    "free": {{DiskFreeSpace}},
                    "threshold": {{512 * OneMegabyte}},
                    "path": {{JsonValue.Create(NetworkPath).ToJsonString()}},
                    "exists": true
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Reports_unknown_when_network_directory_does_not_exist()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:Health:DiskSpace:Path"] = NetworkPath
        };

        var diskSpaceProvider = new FakeDiskSpaceProvider(true, PlatformDrives, NetworkShares, []);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<IDiskSpaceProvider>(diskSpaceProvider);
        builder.Services.AddHealthActuator();
        builder.Services.RemoveAll<IHealthContributor>();
        builder.Services.AddSingleton<IHealthContributor, DiskSpaceHealthContributor>();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson($$"""
            {
              "status": "UNKNOWN",
              "components": {
                "diskSpace": {
                  "status": "UNKNOWN",
                  "description": "Failed to determine free disk space.",
                  "details": {
                    "error": "The configured path is invalid or does not exist.",
                    "path": {{JsonValue.Create(NetworkPath).ToJsonString()}}
                  }
                }
              }
            }
            """);
    }

    [Theory]
    [InlineData(@"C:\", @"C:\", PlatformID.Win32NT)]
    [InlineData(@"C:\Windows\System32", @"C:\", PlatformID.Win32NT)]
    [InlineData(@"C:\Windows\System32\", @"C:\", PlatformID.Win32NT)]
    [InlineData(@"c:\WINDOWS\SYSTEM32\", @"C:\", PlatformID.Win32NT)]
    [InlineData("/", "/", PlatformID.Unix, PlatformID.MacOSX)]
    [InlineData("/dev", "/dev", PlatformID.Unix, PlatformID.MacOSX)]
    [InlineData("/dev/", "/dev", PlatformID.Unix, PlatformID.MacOSX)]
    [InlineData("/dev/shm", "/dev/shm", PlatformID.Unix, PlatformID.MacOSX)]
    [InlineData("/dev/shm/", "/dev/shm", PlatformID.Unix, PlatformID.MacOSX)]
    [InlineData("/dev/shm/data", "/dev/shm", PlatformID.Unix, PlatformID.MacOSX)]
    [InlineData("/dev/shm-some", "/dev", PlatformID.Unix, PlatformID.MacOSX)]
    [InlineData("/dev/SHM", "/dev", PlatformID.Unix)]
    [InlineData("/dev/SHM", "/dev/shm", PlatformID.MacOSX)]
    public void Selects_correct_local_volume(string path, string? expected, params PlatformID[] platforms)
    {
        if (OperatingSystem.IsWindows() && !platforms.Contains(PlatformID.Win32NT))
        {
            return;
        }

        if (OperatingSystem.IsLinux() && !platforms.Contains(PlatformID.Unix))
        {
            return;
        }

        if (OperatingSystem.IsMacOS() && !platforms.Contains(PlatformID.MacOSX))
        {
            return;
        }

        DriveInfoWrapper[] systemDrives = OperatingSystem.IsWindows()
            ?
            [
                new DriveInfoWrapper(new DriveInfo(@"C:\")),
                new DriveInfoWrapper(new DriveInfo(@"D:\"))
            ]
            :
            [
                new DriveInfoWrapper(new DriveInfo("/")),
                new DriveInfoWrapper(new DriveInfo("/dev")),
                new DriveInfoWrapper(new DriveInfo("/dev/shm"))
            ];

        IDriveInfoWrapper? drive = DiskSpaceHealthContributor.FindVolume(path, systemDrives);

        if (expected == null)
        {
            drive.Should().BeNull();
        }
        else
        {
            drive.Should().NotBeNull();
            drive.RootDirectory.FullName.Should().Be(expected);
        }
    }
}

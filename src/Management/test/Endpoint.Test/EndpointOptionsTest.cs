// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.All;
using Steeltoe.Management.Endpoint.Actuators.Environment;
using Steeltoe.Management.Endpoint.Actuators.Info;
using Steeltoe.Management.Endpoint.Actuators.Loggers;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test;

public sealed class EndpointOptionsTest
{
    [Fact]
    public async Task Does_not_register_options_configurer_multiple_times()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);

        services.AddInfoActuator();
        services.AddEnvironmentActuator();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        serviceProvider.GetServices<IConfigureOptions<ManagementOptions>>().Should().ContainSingle();
        serviceProvider.GetServices<IOptionsChangeTokenSource<ManagementOptions>>().Should().ContainSingle();
    }

    [Fact]
    public async Task Can_register_additional_options_configurer_upfront()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);

        services.AddTransient<IConfigureOptions<ManagementOptions>, CustomManagementOptionsConfigurer>();
        services.AddInfoActuator();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        IConfigureOptions<ManagementOptions>[] configurers = [.. serviceProvider.GetServices<IConfigureOptions<ManagementOptions>>()];
        configurers.Should().HaveCount(2);
        configurers.OfType<ConfigureManagementOptions>().Should().ContainSingle();
        configurers.OfType<CustomManagementOptionsConfigurer>().Should().ContainSingle();

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ManagementOptions>>();
        optionsMonitor.CurrentValue.Port.Should().Be(9999);
    }

    [Fact]
    public async Task Can_register_additional_options_configurer_afterwards()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);

        services.AddInfoActuator();
        services.AddTransient<IConfigureOptions<ManagementOptions>, CustomManagementOptionsConfigurer>();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        IConfigureOptions<ManagementOptions>[] configurers = [.. serviceProvider.GetServices<IConfigureOptions<ManagementOptions>>()];
        configurers.Should().HaveCount(2);
        configurers.OfType<ConfigureManagementOptions>().Should().ContainSingle();
        configurers.OfType<CustomManagementOptionsConfigurer>().Should().ContainSingle();

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ManagementOptions>>();
        optionsMonitor.CurrentValue.Port.Should().Be(9999);
    }

    [Fact]
    public async Task CanTurnOffEndpointAtRuntimeFromExposureConfiguration()
    {
        MemoryFileProvider fileProvider = new();

        fileProvider.IncludeFile(MemoryFileProvider.DefaultAppSettingsFileName, """
        {
            "Management:Endpoints:Actuator:Exposure:Include:0": "env"
        }
        """);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddJsonFile(fileProvider, MemoryFileProvider.DefaultAppSettingsFileName, false, true);
        builder.Services.AddAllActuators();

        await using WebApplication app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient httpClient = app.GetTestClient();
        HttpResponseMessage response1 = await httpClient.GetAsync(new Uri("/actuator/env", UriKind.Relative), TestContext.Current.CancellationToken);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        fileProvider.ReplaceFile(MemoryFileProvider.DefaultAppSettingsFileName, """
        {
            "Management:Endpoints:Actuator:Exposure:Include:0": "env",
            "Management:Endpoints:Actuator:Exposure:Exclude:0": "*"
        }
        """);

        fileProvider.NotifyChanged();

        HttpResponseMessage response2 = await httpClient.GetAsync(new Uri("/actuator/env", UriKind.Relative), TestContext.Current.CancellationToken);
        response2.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CanTurnOnEndpointAtRuntimeFromExposureConfiguration()
    {
        MemoryFileProvider fileProvider = new();

        fileProvider.IncludeFile(MemoryFileProvider.DefaultAppSettingsFileName, """
        {
            "Management:Endpoints:Actuator:Exposure:Include:0": "env",
            "Management:Endpoints:Actuator:Exposure:Exclude:0": "*"
        }
        """);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddJsonFile(fileProvider, MemoryFileProvider.DefaultAppSettingsFileName, false, true);
        builder.Services.AddAllActuators();

        await using WebApplication app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient httpClient = app.GetTestClient();
        HttpResponseMessage response1 = await httpClient.GetAsync(new Uri("/actuator/env", UriKind.Relative), TestContext.Current.CancellationToken);
        response1.StatusCode.Should().Be(HttpStatusCode.NotFound);

        fileProvider.ReplaceFile(MemoryFileProvider.DefaultAppSettingsFileName, """
        {
            "Management:Endpoints:Actuator:Exposure:Include:0": "env"
        }
        """);

        fileProvider.NotifyChanged();

        HttpResponseMessage response2 = await httpClient.GetAsync(new Uri("/actuator/env", UriKind.Relative), TestContext.Current.CancellationToken);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CanTurnOffEndpointAtRuntimeFromEndpointConfiguration()
    {
        MemoryFileProvider fileProvider = new();

        fileProvider.IncludeFile(MemoryFileProvider.DefaultAppSettingsFileName, """
        {
            "Management:Endpoints:Actuator:Exposure:Include:0": "env",
            "Management:Endpoints:Env:Enabled": "true"
        }
        """);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddJsonFile(fileProvider, MemoryFileProvider.DefaultAppSettingsFileName, false, true);
        builder.Services.AddAllActuators();

        await using WebApplication app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient httpClient = app.GetTestClient();
        HttpResponseMessage response1 = await httpClient.GetAsync(new Uri("/actuator/env", UriKind.Relative), TestContext.Current.CancellationToken);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        fileProvider.ReplaceFile(MemoryFileProvider.DefaultAppSettingsFileName, """
        {
            "Management:Endpoints:Actuator:Exposure:Include:0": "env",
            "Management:Endpoints:Env:Enabled": "false"
        }
        """);

        fileProvider.NotifyChanged();

        HttpResponseMessage response2 = await httpClient.GetAsync(new Uri("/actuator/env", UriKind.Relative), TestContext.Current.CancellationToken);
        response2.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CanTurnOnEndpointAtRuntimeFromEndpointConfiguration()
    {
        MemoryFileProvider fileProvider = new();

        fileProvider.IncludeFile(MemoryFileProvider.DefaultAppSettingsFileName, """
        {
            "Management:Endpoints:Actuator:Exposure:Include:0": "env",
            "Management:Endpoints:Env:Enabled": "false"
        }
        """);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddJsonFile(fileProvider, MemoryFileProvider.DefaultAppSettingsFileName, false, true);
        builder.Services.AddAllActuators();

        await using WebApplication app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient httpClient = app.GetTestClient();
        HttpResponseMessage response1 = await httpClient.GetAsync(new Uri("/actuator/env", UriKind.Relative), TestContext.Current.CancellationToken);
        response1.StatusCode.Should().Be(HttpStatusCode.NotFound);

        fileProvider.ReplaceFile(MemoryFileProvider.DefaultAppSettingsFileName, """
        {
            "Management:Endpoints:Actuator:Exposure:Include:0": "env",
            "Management:Endpoints:Env:Enabled": "true"
        }
        """);

        fileProvider.NotifyChanged();

        HttpResponseMessage response2 = await httpClient.GetAsync(new Uri("/actuator/env", UriKind.Relative), TestContext.Current.CancellationToken);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(null, null, "/")]
    [InlineData(null, "", "/")]
    [InlineData(null, "/", "/")]
    [InlineData(null, "/ping", "/ping")]
    [InlineData(null, "/ping/", "/ping")]
    [InlineData(null, "ping", "/ping")]
    [InlineData(null, "ping/", "/ping")]
    [InlineData("", null, "/")]
    [InlineData("", "", "/")]
    [InlineData("", "/", "/")]
    [InlineData("", "/ping", "/ping")]
    [InlineData("", "/ping/", "/ping")]
    [InlineData("", "ping", "/ping")]
    [InlineData("", "ping/", "/ping")]
    [InlineData("/", null, "/")]
    [InlineData("/", "", "/")]
    [InlineData("/", "/", "/")]
    [InlineData("/", "/ping", "/ping")]
    [InlineData("/", "/ping/", "/ping")]
    [InlineData("/", "ping", "/ping")]
    [InlineData("/", "ping/", "/ping")]
    [InlineData("/actuator", null, "/actuator")]
    [InlineData("/actuator", "", "/actuator")]
    [InlineData("/actuator", "/", "/actuator")]
    [InlineData("/actuator", "/ping", "/actuator/ping")]
    [InlineData("/actuator", "/ping/", "/actuator/ping")]
    [InlineData("/actuator", "ping", "/actuator/ping")]
    [InlineData("/actuator", "ping/", "/actuator/ping")]
    [InlineData("/actuator/", null, "/actuator")]
    [InlineData("/actuator/", "", "/actuator")]
    [InlineData("/actuator/", "/", "/actuator")]
    [InlineData("/actuator/", "/ping", "/actuator/ping")]
    [InlineData("/actuator/", "/ping/", "/actuator/ping")]
    [InlineData("/actuator/", "ping", "/actuator/ping")]
    [InlineData("/actuator/", "ping/", "/actuator/ping")]
    [InlineData("actuator", null, "/actuator")]
    [InlineData("actuator", "", "/actuator")]
    [InlineData("actuator", "/", "/actuator")]
    [InlineData("actuator", "/ping", "/actuator/ping")]
    [InlineData("actuator", "/ping/", "/actuator/ping")]
    [InlineData("actuator", "ping", "/actuator/ping")]
    [InlineData("actuator", "ping/", "/actuator/ping")]
    [InlineData("actuator/", null, "/actuator")]
    [InlineData("actuator/", "", "/actuator")]
    [InlineData("actuator/", "/", "/actuator")]
    [InlineData("actuator/", "/ping", "/actuator/ping")]
    [InlineData("actuator/", "/ping/", "/actuator/ping")]
    [InlineData("actuator/", "ping", "/actuator/ping")]
    [InlineData("actuator/", "ping/", "/actuator/ping")]
    public void GetEndpointPath_combines_segments(string? managementPath, string? endpointPath, string expected)
    {
        var endpointOptions = new RouteMappingsEndpointOptions
        {
            Path = endpointPath
        };

        string result = endpointOptions.GetEndpointPath(managementPath);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(false, false, false, false, false)]
    [InlineData(false, false, false, true, false)]
    [InlineData(false, false, true, false, false)]
    [InlineData(false, false, true, true, true)]
    [InlineData(false, true, false, false, false)]
    [InlineData(false, true, false, true, false)]
    [InlineData(false, true, true, false, false)]
    [InlineData(false, true, true, true, true)]
    [InlineData(true, false, false, false, false)]
    [InlineData(true, false, false, true, false)]
    [InlineData(true, false, true, false, false)]
    [InlineData(true, false, true, true, false)]
    [InlineData(true, true, false, false, false)]
    [InlineData(true, true, false, true, true)]
    [InlineData(true, true, true, false, false)]
    [InlineData(true, true, true, true, true)]
    public async Task CanInvoke_returns_expected(bool isCloudFoundryEndpoint, bool hasCloudFoundrySecurity, bool isExposed, bool isEnabled, bool canInvoke)
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Actuator:Exposure:Include:0"] = isExposed ? "loggers" : "beans",
            ["Management:Endpoints:Loggers:Enabled"] = isEnabled ? "true" : "false"
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddLoggersActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        ManagementOptions managementOptions = serviceProvider.GetRequiredService<IOptions<ManagementOptions>>().Value;
        LoggersEndpointOptions endpointOptions = serviceProvider.GetRequiredService<IOptions<LoggersEndpointOptions>>().Value;
        string requestPath = isCloudFoundryEndpoint ? "/cloudfoundryapplication/loggers" : "/actuators/loggers";

        if (hasCloudFoundrySecurity)
        {
            managementOptions.HasCloudFoundrySecurity = true;
        }

        endpointOptions.CanInvoke(requestPath, managementOptions).Should().Be(canInvoke);
    }

    private sealed class CustomManagementOptionsConfigurer : IConfigureOptions<ManagementOptions>
    {
        public void Configure(ManagementOptions options)
        {
            options.Port = 9999;
        }
    }
}

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
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test;

public sealed class ConfigureOptionsTest
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

        IConfigureOptions<ManagementOptions>[] configurers = [.. serviceProvider.GetServices<IConfigureOptions<ManagementOptions>>()];
        configurers.Should().ContainSingle();

        IOptionsChangeTokenSource<ManagementOptions>[] tokenSources = [.. serviceProvider.GetServices<IOptionsChangeTokenSource<ManagementOptions>>()];
        tokenSources.Should().ContainSingle();
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
        optionsMonitor.CurrentValue.Port.Should().Be("9999");
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
        optionsMonitor.CurrentValue.Port.Should().Be("9999");
    }

    [Fact]
    public async Task CanTurnOffEndpointAtRuntimeFromExposureConfiguration()
    {
        const string fileName = "appsettings.json";
        MemoryFileProvider fileProvider = new();

        fileProvider.IncludeFile(fileName, """
        {
            "Management:Endpoints:Actuator:Exposure:Include:0": "env"
        }
        """);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddJsonFile(fileProvider, fileName, false, true);
        builder.Services.AddAllActuators();

        await using WebApplication app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient httpClient = app.GetTestClient();
        HttpResponseMessage response1 = await httpClient.GetAsync(new Uri("/actuator/env", UriKind.Relative), TestContext.Current.CancellationToken);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        fileProvider.ReplaceFile(fileName, """
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
        const string fileName = "appsettings.json";
        MemoryFileProvider fileProvider = new();

        fileProvider.IncludeFile(fileName, """
        {
            "Management:Endpoints:Actuator:Exposure:Include:0": "env",
            "Management:Endpoints:Actuator:Exposure:Exclude:0": "*"
        }
        """);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddJsonFile(fileProvider, fileName, false, true);
        builder.Services.AddAllActuators();

        await using WebApplication app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient httpClient = app.GetTestClient();
        HttpResponseMessage response1 = await httpClient.GetAsync(new Uri("/actuator/env", UriKind.Relative), TestContext.Current.CancellationToken);
        response1.StatusCode.Should().Be(HttpStatusCode.NotFound);

        fileProvider.ReplaceFile(fileName, """
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
        const string fileName = "appsettings.json";
        MemoryFileProvider fileProvider = new();

        fileProvider.IncludeFile(fileName, """
        {
            "Management:Endpoints:Actuator:Exposure:Include:0": "env",
            "Management:Endpoints:Env:Enabled": "true"
        }
        """);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddJsonFile(fileProvider, fileName, false, true);
        builder.Services.AddAllActuators();

        await using WebApplication app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient httpClient = app.GetTestClient();
        HttpResponseMessage response1 = await httpClient.GetAsync(new Uri("/actuator/env", UriKind.Relative), TestContext.Current.CancellationToken);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        fileProvider.ReplaceFile(fileName, """
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
        const string fileName = "appsettings.json";
        MemoryFileProvider fileProvider = new();

        fileProvider.IncludeFile(fileName, """
        {
            "Management:Endpoints:Actuator:Exposure:Include:0": "env",
            "Management:Endpoints:Env:Enabled": "false"
        }
        """);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddJsonFile(fileProvider, fileName, false, true);
        builder.Services.AddAllActuators();

        await using WebApplication app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient httpClient = app.GetTestClient();
        HttpResponseMessage response1 = await httpClient.GetAsync(new Uri("/actuator/env", UriKind.Relative), TestContext.Current.CancellationToken);
        response1.StatusCode.Should().Be(HttpStatusCode.NotFound);

        fileProvider.ReplaceFile(fileName, """
        {
            "Management:Endpoints:Actuator:Exposure:Include:0": "env",
            "Management:Endpoints:Env:Enabled": "true"
        }
        """);

        fileProvider.NotifyChanged();

        HttpResponseMessage response2 = await httpClient.GetAsync(new Uri("/actuator/env", UriKind.Relative), TestContext.Current.CancellationToken);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed class CustomManagementOptionsConfigurer : IConfigureOptions<ManagementOptions>
    {
        public void Configure(ManagementOptions options)
        {
            options.Port = "9999";
        }
    }
}

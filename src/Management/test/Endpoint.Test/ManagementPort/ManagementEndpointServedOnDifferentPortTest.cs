// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Management.Endpoint.Test.ManagementPort;

public sealed class ManagementEndpointServedOnDifferentPortTest
{
    private const string AspNetDefaultPort = "5000";

    [Fact]
    public async Task AspNetDefaultPort_NoManagementPortConfigured_BothAccessibleOnSamePort()
    {
        await using WebApplication app = await CreateAppAsync(new Dictionary<string, string?>());

        IFeatureCollection serverFeatures = app.Services.GetRequiredService<IServer>().Features;
        ICollection<string> addresses = serverFeatures.GetRequiredFeature<IServerAddressesFeature>().Addresses;

        addresses.Should().HaveCount(1);
        addresses.ElementAt(0).Should().Be($"http://localhost:{AspNetDefaultPort}");

        using HttpClient httpClient = CreateHttpClient();

        HttpResponseMessage appResponse = await httpClient.GetAsync(new Uri($"http://localhost:{AspNetDefaultPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage actuatorResponse = await httpClient.GetAsync(new Uri($"http://localhost:{AspNetDefaultPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AspNetDefaultPort_SameManagementPortConfigured_OnlyActuatorAccessible()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:port"] = AspNetDefaultPort
        };

        await using WebApplication app = await CreateAppAsync(appSettings);

        IFeatureCollection serverFeatures = app.Services.GetRequiredService<IServer>().Features;
        ICollection<string> addresses = serverFeatures.GetRequiredFeature<IServerAddressesFeature>().Addresses;

        addresses.Should().HaveCount(1);
        addresses.ElementAt(0).Should().Be($"http://[::]:{AspNetDefaultPort}");

        using HttpClient httpClient = CreateHttpClient();

        HttpResponseMessage appResponse = await httpClient.GetAsync(new Uri($"http://localhost:{AspNetDefaultPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        HttpResponseMessage actuatorResponse = await httpClient.GetAsync(new Uri($"http://localhost:{AspNetDefaultPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AspNetDefaultPort_AlternateManagementPortConfigured_AccessibleOnSeparatePorts()
    {
        const string managementPort = "8000";

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:port"] = managementPort
        };

        await using WebApplication app = await CreateAppAsync(appSettings);

        IFeatureCollection serverFeatures = app.Services.GetRequiredService<IServer>().Features;
        ICollection<string> addresses = serverFeatures.GetRequiredFeature<IServerAddressesFeature>().Addresses;

        addresses.Should().HaveCount(2);
        addresses.ElementAt(0).Should().Be($"http://localhost:{AspNetDefaultPort}");
        addresses.ElementAt(1).Should().Be($"http://[::]:{managementPort}");

        using HttpClient httpClient = CreateHttpClient();

        HttpResponseMessage appResponse = await httpClient.GetAsync(new Uri($"http://localhost:{AspNetDefaultPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        appResponse = await httpClient.GetAsync(new Uri($"http://localhost:{managementPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        HttpResponseMessage actuatorResponse = await httpClient.GetAsync(new Uri($"http://localhost:{AspNetDefaultPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        actuatorResponse = await httpClient.GetAsync(new Uri($"http://localhost:{managementPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")] // https://github.com/dotnet/aspnetcore/issues/42273
    public async Task AspNetDefaultPort_AlternateManagementPortAndSchemeConfigured_AccessibleOnSeparatePortsAndSchemes()
    {
        const string managementPort = "8000";

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:port"] = managementPort,
            ["management:endpoints:sslEnabled"] = "true"
        };

        await using WebApplication app = await CreateAppAsync(appSettings);

        IFeatureCollection serverFeatures = app.Services.GetRequiredService<IServer>().Features;
        ICollection<string> addresses = serverFeatures.GetRequiredFeature<IServerAddressesFeature>().Addresses;

        addresses.Should().HaveCount(2);
        addresses.ElementAt(0).Should().Be($"http://localhost:{AspNetDefaultPort}");
        addresses.ElementAt(1).Should().Be($"https://[::]:{managementPort}");

        using HttpClient httpClient = CreateHttpClient();

        HttpResponseMessage appResponse = await httpClient.GetAsync(new Uri($"http://localhost:{AspNetDefaultPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        appResponse = await httpClient.GetAsync(new Uri($"https://localhost:{managementPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        HttpResponseMessage actuatorResponse = await httpClient.GetAsync(new Uri($"http://localhost:{AspNetDefaultPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        actuatorResponse = await httpClient.GetAsync(new Uri($"https://localhost:{managementPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")] // https://github.com/dotnet/aspnetcore/issues/42273
    public async Task AspNetCustomPortsConfigured_NoManagementPortConfigured_BothAccessibleOnSamePort()
    {
        const string appHttpPort = "6000";
        const string appHttpsPort = "7000";

        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = $"http://localhost:{appHttpPort};https://*:{appHttpsPort}"
        };

        await using WebApplication app = await CreateAppAsync(appSettings);

        IFeatureCollection serverFeatures = app.Services.GetRequiredService<IServer>().Features;
        ICollection<string> addresses = serverFeatures.GetRequiredFeature<IServerAddressesFeature>().Addresses;

        addresses.Should().HaveCount(2);
        addresses.ElementAt(0).Should().Be($"http://localhost:{appHttpPort}");
        addresses.ElementAt(1).Should().Be($"https://[::]:{appHttpsPort}");

        using HttpClient httpClient = CreateHttpClient();

        HttpResponseMessage appResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        appResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage actuatorResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        actuatorResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")] // https://github.com/dotnet/aspnetcore/issues/42273
    public async Task AspNetCustomPortsConfigured_SameManagementPortConfigured_OnlyActuatorAccessible()
    {
        const string appHttpPort = "6000";
        const string appHttpsPort = "7000";

        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = $"http://localhost:{appHttpPort};https://*:{appHttpsPort}",
            ["management:endpoints:port"] = appHttpPort
        };

        await using WebApplication app = await CreateAppAsync(appSettings);

        IFeatureCollection serverFeatures = app.Services.GetRequiredService<IServer>().Features;
        ICollection<string> addresses = serverFeatures.GetRequiredFeature<IServerAddressesFeature>().Addresses;

        addresses.Should().HaveCount(2);
        addresses.ElementAt(0).Should().Be($"http://localhost:{appHttpPort}");
        addresses.ElementAt(1).Should().Be($"https://[::]:{appHttpsPort}");

        using HttpClient httpClient = CreateHttpClient();

        HttpResponseMessage appResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        appResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage actuatorResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        actuatorResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")] // https://github.com/dotnet/aspnetcore/issues/42273
    public async Task AspNetCustomPortsConfigured_AlternateManagementPortConfigured_AccessibleOnSeparatePorts()
    {
        const string appHttpPort = "6000";
        const string appHttpsPort = "7000";
        const string managementPort = "8000";

        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = $"http://localhost:{appHttpPort};https://*:{appHttpsPort}",
            ["management:endpoints:port"] = managementPort
        };

        await using WebApplication app = await CreateAppAsync(appSettings);

        IFeatureCollection serverFeatures = app.Services.GetRequiredService<IServer>().Features;
        ICollection<string> addresses = serverFeatures.GetRequiredFeature<IServerAddressesFeature>().Addresses;

        addresses.Should().HaveCount(3);
        addresses.ElementAt(0).Should().Be($"http://localhost:{appHttpPort}");
        addresses.ElementAt(1).Should().Be($"https://[::]:{appHttpsPort}");
        addresses.ElementAt(2).Should().Be($"http://[::]:{managementPort}");

        using HttpClient httpClient = CreateHttpClient();

        HttpResponseMessage appResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        appResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        appResponse = await httpClient.GetAsync(new Uri($"http://localhost:{managementPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        HttpResponseMessage actuatorResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        actuatorResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        actuatorResponse = await httpClient.GetAsync(new Uri($"http://localhost:{managementPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")] // https://github.com/dotnet/aspnetcore/issues/42273
    public async Task AspNetCustomPortsConfigured_AlternateHttpsManagementPortConfigured_AccessibleOnSeparatePortsAndSchemes()
    {
        const string appHttpPort = "6000";
        const string appHttpsPort = "7000";
        const string managementPort = "8000";

        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = $"http://localhost:{appHttpPort};https://*:{appHttpsPort}",
            ["management:endpoints:port"] = managementPort,
            ["management:endpoints:sslEnabled"] = "true"
        };

        await using WebApplication app = await CreateAppAsync(appSettings);

        IFeatureCollection serverFeatures = app.Services.GetRequiredService<IServer>().Features;
        ICollection<string> addresses = serverFeatures.GetRequiredFeature<IServerAddressesFeature>().Addresses;

        addresses.Should().HaveCount(3);
        addresses.ElementAt(0).Should().Be($"http://localhost:{appHttpPort}");
        addresses.ElementAt(1).Should().Be($"https://[::]:{appHttpsPort}");
        addresses.ElementAt(2).Should().Be($"https://[::]:{managementPort}");

        using HttpClient httpClient = CreateHttpClient();

        HttpResponseMessage appResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        appResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        appResponse = await httpClient.GetAsync(new Uri($"https://localhost:{managementPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        HttpResponseMessage actuatorResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        actuatorResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        actuatorResponse = await httpClient.GetAsync(new Uri($"https://localhost:{managementPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")] // https://github.com/dotnet/aspnetcore/issues/42273
    public async Task AspNetCustomPortsInEnvironmentVariables_NoManagementPortConfigured_BothAccessibleOnSamePort()
    {
        const string appHttpPort = "6000";
        const string appHttpsPort = "7000";

        using var httpScope = new EnvironmentVariableScope("HTTP_PORTS", appHttpPort);
        using var httpsScope = new EnvironmentVariableScope("HTTPS_PORTS", appHttpsPort);

        await using WebApplication app = await CreateAppAsync(new Dictionary<string, string?>());

        IFeatureCollection serverFeatures = app.Services.GetRequiredService<IServer>().Features;
        ICollection<string> addresses = serverFeatures.GetRequiredFeature<IServerAddressesFeature>().Addresses;

        addresses.Should().HaveCount(2);
        addresses.ElementAt(0).Should().Be($"http://[::]:{appHttpPort}");
        addresses.ElementAt(1).Should().Be($"https://[::]:{appHttpsPort}");

        using HttpClient httpClient = CreateHttpClient();

        HttpResponseMessage appResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        appResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage actuatorResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        actuatorResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")] // https://github.com/dotnet/aspnetcore/issues/42273
    public async Task AspNetCustomPortsInEnvironmentVariables_SameManagementPortConfigured_OnlyActuatorAccessible()
    {
        const string appHttpPort = "6000";
        const string appHttpsPort = "7000";

        using var httpScope = new EnvironmentVariableScope("HTTP_PORTS", appHttpPort);
        using var httpsScope = new EnvironmentVariableScope("HTTPS_PORTS", appHttpsPort);

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:port"] = appHttpPort
        };

        await using WebApplication app = await CreateAppAsync(appSettings);

        IFeatureCollection serverFeatures = app.Services.GetRequiredService<IServer>().Features;
        ICollection<string> addresses = serverFeatures.GetRequiredFeature<IServerAddressesFeature>().Addresses;

        addresses.Should().HaveCount(2);
        addresses.ElementAt(0).Should().Be($"http://[::]:{appHttpPort}");
        addresses.ElementAt(1).Should().Be($"https://[::]:{appHttpsPort}");

        using HttpClient httpClient = CreateHttpClient();

        HttpResponseMessage appResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        appResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage actuatorResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        actuatorResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")] // https://github.com/dotnet/aspnetcore/issues/42273
    public async Task AspNetCustomPortsInEnvironmentVariables_AlternateManagementPortConfigured_AccessibleOnSeparatePorts()
    {
        const string appHttpPort = "6000";
        const string appHttpsPort = "7000";
        const string managementPort = "8000";

        using var httpScope = new EnvironmentVariableScope("HTTP_PORTS", appHttpPort);
        using var httpsScope = new EnvironmentVariableScope("HTTPS_PORTS", appHttpsPort);

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:port"] = managementPort
        };

        await using WebApplication app = await CreateAppAsync(appSettings);

        IFeatureCollection serverFeatures = app.Services.GetRequiredService<IServer>().Features;
        ICollection<string> addresses = serverFeatures.GetRequiredFeature<IServerAddressesFeature>().Addresses;

        addresses.Should().HaveCount(3);
        addresses.ElementAt(0).Should().Be($"http://[::]:{appHttpPort}");
        addresses.ElementAt(1).Should().Be($"https://[::]:{appHttpsPort}");
        addresses.ElementAt(2).Should().Be($"http://[::]:{managementPort}");

        using HttpClient httpClient = CreateHttpClient();

        HttpResponseMessage appResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        appResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        appResponse = await httpClient.GetAsync(new Uri($"http://localhost:{managementPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        HttpResponseMessage actuatorResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        actuatorResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        actuatorResponse = await httpClient.GetAsync(new Uri($"http://localhost:{managementPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")] // https://github.com/dotnet/aspnetcore/issues/42273
    public async Task AspNetCustomPortsInEnvironmentVariables_AlternateHttpsManagementPortConfigured_AccessibleOnSeparatePortsAndSchemes()
    {
        const string appHttpPort = "6000";
        const string appHttpsPort = "7000";
        const string managementPort = "8000";

        using var httpScope = new EnvironmentVariableScope("HTTP_PORTS", appHttpPort);
        using var httpsScope = new EnvironmentVariableScope("HTTPS_PORTS", appHttpsPort);

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:port"] = managementPort,
            ["management:endpoints:sslEnabled"] = "true"
        };

        await using WebApplication app = await CreateAppAsync(appSettings);

        IFeatureCollection serverFeatures = app.Services.GetRequiredService<IServer>().Features;
        ICollection<string> addresses = serverFeatures.GetRequiredFeature<IServerAddressesFeature>().Addresses;

        addresses.Should().HaveCount(3);
        addresses.ElementAt(0).Should().Be($"http://[::]:{appHttpPort}");
        addresses.ElementAt(1).Should().Be($"https://[::]:{appHttpsPort}");
        addresses.ElementAt(2).Should().Be($"https://[::]:{managementPort}");

        using HttpClient httpClient = CreateHttpClient();

        HttpResponseMessage appResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        appResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        appResponse = await httpClient.GetAsync(new Uri($"https://localhost:{managementPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        HttpResponseMessage actuatorResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        actuatorResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        actuatorResponse = await httpClient.GetAsync(new Uri($"https://localhost:{managementPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")] // https://github.com/dotnet/aspnetcore/issues/42273
    public async Task AspNetCustomPortsInCode_NoManagementPortConfigured_BothAccessibleOnSamePort()
    {
        const string appHttpPort = "6000";
        const string appHttpsPort = "7000";

        await using WebApplication app = await CreateAppAsync(new Dictionary<string, string?>(), app =>
        {
            app.Urls.Add($"http://localhost:{appHttpPort}");
            app.Urls.Add($"https://*:{appHttpsPort}");
        });

        IFeatureCollection serverFeatures = app.Services.GetRequiredService<IServer>().Features;
        ICollection<string> addresses = serverFeatures.GetRequiredFeature<IServerAddressesFeature>().Addresses;

        addresses.Should().HaveCount(2);
        addresses.ElementAt(0).Should().Be($"http://localhost:{appHttpPort}");
        addresses.ElementAt(1).Should().Be($"https://[::]:{appHttpsPort}");

        using HttpClient httpClient = CreateHttpClient();

        HttpResponseMessage appResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        appResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage actuatorResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        actuatorResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")] // https://github.com/dotnet/aspnetcore/issues/42273
    public async Task AspNetCustomPortsInCode_SameManagementPortConfigured_OnlyActuatorAccessible()
    {
        const string appHttpPort = "6000";
        const string appHttpsPort = "7000";

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:port"] = appHttpPort
        };

        await using WebApplication app = await CreateAppAsync(appSettings, app =>
        {
            app.Urls.Add($"http://localhost:{appHttpPort}");
            app.Urls.Add($"https://*:{appHttpsPort}");
        });

        IFeatureCollection serverFeatures = app.Services.GetRequiredService<IServer>().Features;
        ICollection<string> addresses = serverFeatures.GetRequiredFeature<IServerAddressesFeature>().Addresses;

        addresses.Should().HaveCount(2);
        addresses.ElementAt(0).Should().Be($"http://localhost:{appHttpPort}");
        addresses.ElementAt(1).Should().Be($"https://[::]:{appHttpsPort}");

        using HttpClient httpClient = CreateHttpClient();

        HttpResponseMessage appResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        appResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage actuatorResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        actuatorResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")] // https://github.com/dotnet/aspnetcore/issues/42273
    public async Task AspNetCustomPortsInCode_AlternateManagementPortConfigured_AccessibleOnSeparatePorts()
    {
        const string appHttpPort = "6000";
        const string appHttpsPort = "7000";
        const string managementPort = "8000";

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:port"] = managementPort
        };

        await using WebApplication app = await CreateAppAsync(appSettings, app =>
        {
            app.Urls.Add($"http://localhost:{appHttpPort}");
            app.Urls.Add($"https://*:{appHttpsPort}");
        });

        IFeatureCollection serverFeatures = app.Services.GetRequiredService<IServer>().Features;
        ICollection<string> addresses = serverFeatures.GetRequiredFeature<IServerAddressesFeature>().Addresses;

        addresses.Should().HaveCount(3);
        addresses.ElementAt(0).Should().Be($"http://localhost:{appHttpPort}");
        addresses.ElementAt(1).Should().Be($"https://[::]:{appHttpsPort}");
        addresses.ElementAt(2).Should().Be($"http://[::]:{managementPort}");

        using HttpClient httpClient = CreateHttpClient();

        HttpResponseMessage appResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        appResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        appResponse = await httpClient.GetAsync(new Uri($"http://localhost:{managementPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        HttpResponseMessage actuatorResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        actuatorResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        actuatorResponse = await httpClient.GetAsync(new Uri($"http://localhost:{managementPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")] // https://github.com/dotnet/aspnetcore/issues/42273
    public async Task AspNetCustomPortsInCode_AlternateHttpsManagementPortConfigured_AccessibleOnSeparatePortsAndSchemes()
    {
        const string appHttpPort = "6000";
        const string appHttpsPort = "7000";
        const string managementPort = "8000";

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:port"] = managementPort,
            ["management:endpoints:sslEnabled"] = "true"
        };

        await using WebApplication app = await CreateAppAsync(appSettings, app =>
        {
            app.Urls.Add($"http://localhost:{appHttpPort}");
            app.Urls.Add($"https://*:{appHttpsPort}");
        });

        IFeatureCollection serverFeatures = app.Services.GetRequiredService<IServer>().Features;
        ICollection<string> addresses = serverFeatures.GetRequiredFeature<IServerAddressesFeature>().Addresses;

        addresses.Should().HaveCount(3);
        addresses.ElementAt(0).Should().Be($"http://localhost:{appHttpPort}");
        addresses.ElementAt(1).Should().Be($"https://[::]:{appHttpsPort}");
        addresses.ElementAt(2).Should().Be($"https://[::]:{managementPort}");

        using HttpClient httpClient = CreateHttpClient();

        HttpResponseMessage appResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        appResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        appResponse = await httpClient.GetAsync(new Uri($"https://localhost:{managementPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        HttpResponseMessage actuatorResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        actuatorResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        actuatorResponse = await httpClient.GetAsync(new Uri($"https://localhost:{managementPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")] // https://github.com/dotnet/aspnetcore/issues/42273
    public async Task AspNetDynamicPortsConfigured_NoManagementPortConfigured_BothAccessibleOnSamePort()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = "http://127.0.0.1:0;https://*:0"
        };

        await using WebApplication app = await CreateAppAsync(appSettings);

        IFeatureCollection serverFeatures = app.Services.GetRequiredService<IServer>().Features;
        ICollection<string> addresses = serverFeatures.GetRequiredFeature<IServerAddressesFeature>().Addresses;

        addresses.Should().HaveCount(2);

        int appHttpPort = BindingAddress.Parse(addresses.ElementAt(0)).Port;
        int appHttpsPort = BindingAddress.Parse(addresses.ElementAt(1)).Port;

        addresses.ElementAt(0).Should().Be($"http://127.0.0.1:{appHttpPort}");
        addresses.ElementAt(1).Should().Be($"https://[::]:{appHttpsPort}");

        using HttpClient httpClient = CreateHttpClient();

        HttpResponseMessage appResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        appResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage actuatorResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        actuatorResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")] // https://github.com/dotnet/aspnetcore/issues/42273
    public async Task AspNetDynamicPortsConfigured_AlternateManagementPortConfigured_AccessibleOnSeparatePorts()
    {
        const string managementPort = "8000";

        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = "http://127.0.0.1:0;https://*:0",
            ["management:endpoints:port"] = managementPort
        };

        await using WebApplication app = await CreateAppAsync(appSettings);

        IFeatureCollection serverFeatures = app.Services.GetRequiredService<IServer>().Features;
        ICollection<string> addresses = serverFeatures.GetRequiredFeature<IServerAddressesFeature>().Addresses;

        addresses.Should().HaveCount(3);

        int appHttpPort = BindingAddress.Parse(addresses.ElementAt(0)).Port;
        int appHttpsPort = BindingAddress.Parse(addresses.ElementAt(1)).Port;

        addresses.ElementAt(0).Should().Be($"http://127.0.0.1:{appHttpPort}");
        addresses.ElementAt(1).Should().Be($"https://[::]:{appHttpsPort}");
        addresses.ElementAt(2).Should().Be($"http://[::]:{managementPort}");

        using HttpClient httpClient = CreateHttpClient();

        HttpResponseMessage appResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        appResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        appResponse = await httpClient.GetAsync(new Uri($"http://localhost:{managementPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        HttpResponseMessage actuatorResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        actuatorResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        actuatorResponse = await httpClient.GetAsync(new Uri($"http://localhost:{managementPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")] // https://github.com/dotnet/aspnetcore/issues/42273
    public async Task AspNetDynamicPortsConfigured_AlternateHttpsManagementPortConfigured_AccessibleOnSeparatePortsAndSchemes()
    {
        const string managementPort = "8000";

        var appSettings = new Dictionary<string, string?>
        {
            ["urls"] = "http://127.0.0.1:0;https://*:0",
            ["management:endpoints:port"] = managementPort,
            ["management:endpoints:sslEnabled"] = "true"
        };

        await using WebApplication app = await CreateAppAsync(appSettings);

        IFeatureCollection serverFeatures = app.Services.GetRequiredService<IServer>().Features;
        ICollection<string> addresses = serverFeatures.GetRequiredFeature<IServerAddressesFeature>().Addresses;

        addresses.Should().HaveCount(3);

        int appHttpPort = BindingAddress.Parse(addresses.ElementAt(0)).Port;
        int appHttpsPort = BindingAddress.Parse(addresses.ElementAt(1)).Port;

        addresses.ElementAt(0).Should().Be($"http://127.0.0.1:{appHttpPort}");
        addresses.ElementAt(1).Should().Be($"https://[::]:{appHttpsPort}");
        addresses.ElementAt(2).Should().Be($"https://[::]:{managementPort}");

        using HttpClient httpClient = CreateHttpClient();

        HttpResponseMessage appResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        appResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        appResponse = await httpClient.GetAsync(new Uri($"https://localhost:{managementPort}"));
        appResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        HttpResponseMessage actuatorResponse = await httpClient.GetAsync(new Uri($"http://localhost:{appHttpPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        actuatorResponse = await httpClient.GetAsync(new Uri($"https://localhost:{appHttpsPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        actuatorResponse = await httpClient.GetAsync(new Uri($"https://localhost:{managementPort}/actuator"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<WebApplication> CreateAppAsync(Dictionary<string, string?> appSettings, Action<WebApplication>? configureApp = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Host.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddActionDescriptorCollectionProviderMock();
        builder.AddAllActuators();

        WebApplication app = builder.Build();
        configureApp?.Invoke(app);

        app.MapGet("/", () => "Hello World!");
        await app.StartAsync();

        return app;
    }

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            // In ci-build, the dev cert is generated, but not trusted.
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        return new HttpClient(handler, true);
    }
}

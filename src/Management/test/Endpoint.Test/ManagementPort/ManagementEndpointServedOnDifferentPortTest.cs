// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Hosting;
using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.ManagementPort;

public sealed class ManagementEndpointServedOnDifferentPortTest
{
    [Fact]
    public async Task AddAllActuators_WebApplication_ManagementPortIsSet()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "management:endpoints:port", "9090" }
        };

        WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.Host.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        hostBuilder.Configuration.AddInMemoryCollection(appSettings);
        hostBuilder.AddAllActuators();
        hostBuilder.Services.AddActionDescriptorCollectionProvider();

        await using WebApplication app = hostBuilder.Build();
        app.MapGet("/", () => "Hello World!");
        app.Start();

        var addressFeature = ((IApplicationBuilder)app).ServerFeatures.Get<IServerAddressesFeature>();
        addressFeature.Should().NotBeNull();
        addressFeature!.Addresses.Should().ContainSingle(address => address.Contains(":9090", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AddAllActuators_WebApplication_MiddlewareOnlyAllowsManagementPort()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "management:endpoints:port", "9090" }
        };

        WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.Host.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        hostBuilder.Configuration.AddInMemoryCollection(appSettings);
        hostBuilder.AddAllActuators();
        hostBuilder.Services.AddActionDescriptorCollectionProvider();

        await using WebApplication app = hostBuilder.Build();
        app.MapGet("/", () => "Hello World!");
        app.Start();

        using var httpClient = new HttpClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost:9090/actuator"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await httpClient.GetAsync(new Uri("http://localhost:8080/actuator"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddAllActuators_WebApplicationWithUseCloudHosting_ManagementPortIsSet()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "management:endpoints:port", "9090" }
        };

        WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.Host.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        hostBuilder.Configuration.AddInMemoryCollection(appSettings);
        hostBuilder.UseCloudHosting();
        hostBuilder.AddAllActuators();
        hostBuilder.Services.AddActionDescriptorCollectionProvider();

        await using WebApplication app = hostBuilder.Build();
        app.MapGet("/", () => "Hello World!");
        app.Start();

        var addressFeature = ((IApplicationBuilder)app).ServerFeatures.Get<IServerAddressesFeature>();
        addressFeature.Should().NotBeNull();
        addressFeature!.Addresses.Should().ContainSingle(address => address.Contains(":9090", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AddAllActuators_WebApplicationWithUseCloudHosting_MiddlewareOnlyAllowsManagementPort()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "management:endpoints:port", "9090" }
        };

        WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.Host.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        hostBuilder.Configuration.AddInMemoryCollection(appSettings);
        hostBuilder.UseCloudHosting();
        hostBuilder.AddAllActuators();
        hostBuilder.Services.AddActionDescriptorCollectionProvider();

        await using WebApplication app = hostBuilder.Build();
        app.MapGet("/", () => "Hello World!");
        app.Start();

        using var httpClient = new HttpClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost:9090/actuator"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await httpClient.GetAsync(new Uri("http://localhost:8080/actuator"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")] // https://github.com/dotnet/aspnetcore/issues/42273
    public async Task AddAllActuators_WebApplication_RequiresSSL()
    {
        using var scope1 = new EnvironmentVariableScope("ASPNETCORE_URLS", null);
        using var scope2 = new EnvironmentVariableScope("PORT", null);

        var appSettings = new Dictionary<string, string?>
        {
            { "management:endpoints:port", "9090" },
            { "management:endpoints:sslenabled", "true" }
        };

        WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.Host.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        hostBuilder.Configuration.AddInMemoryCollection(appSettings);
        hostBuilder.AddAllActuators();
        hostBuilder.Services.AddActionDescriptorCollectionProvider();

        await using WebApplication app = hostBuilder.Build();
        app.MapGet("/", () => "Hello World!");
        app.Start();

        using var httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("https://localhost:9090/actuator"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await httpClient.GetAsync(new Uri("http://localhost:8080/actuator"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void AddAllActuators_GenericHost_ManagementPortIsSet()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.UseDefaultServiceProvider(options => options.ValidateScopes = true);
            webHostBuilder.ConfigureServices(services => services.AddRouting().AddActionDescriptorCollectionProvider());
            webHostBuilder.UseSetting("management:endpoints:port", "9090");
            webHostBuilder.AddAllActuators();
            webHostBuilder.Configure(app => app.Run(async context => await context.Response.WriteAsync("Hello World!")));
            webHostBuilder.Configure(applicationBuilder => applicationBuilder.UseRouting());
            webHostBuilder.UseKestrel();
        });

        using IHost host = hostBuilder.Build();
        host.Start();

        var addressFeature = host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        addressFeature.Should().NotBeNull();
        addressFeature!.Addresses.Should().ContainSingle(address => address.Contains(":9090", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AddAllActuators_GenericHost_MiddlewareOnlyAllowsManagementPort()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.UseDefaultServiceProvider(options => options.ValidateScopes = true);
            webHostBuilder.ConfigureServices(services => services.AddRouting().AddActionDescriptorCollectionProvider());
            webHostBuilder.UseSetting("management:endpoints:port", "9090");
            webHostBuilder.AddAllActuators();
            webHostBuilder.Configure(app => app.Run(async context => await context.Response.WriteAsync("Hello World!")));
            webHostBuilder.Configure(applicationBuilder => applicationBuilder.UseRouting());
            webHostBuilder.UseKestrel();
        });

        using IHost host = hostBuilder.Build();
        host.Start();

        using var httpClient = new HttpClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost:9090/actuator"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await httpClient.GetAsync(new Uri("http://localhost:8080/actuator"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void AddAllActuators_GenericHostWithUseCloudHosting_ManagementPortIsSet()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.UseDefaultServiceProvider(options => options.ValidateScopes = true);
            webHostBuilder.ConfigureServices(services => services.AddRouting().AddActionDescriptorCollectionProvider());
            webHostBuilder.UseSetting("management:endpoints:port", "9090");
            webHostBuilder.UseCloudHosting();
            webHostBuilder.AddAllActuators();
            webHostBuilder.Configure(app => app.Run(async context => await context.Response.WriteAsync("Hello World!")));
            webHostBuilder.Configure(applicationBuilder => applicationBuilder.UseRouting());
            webHostBuilder.UseKestrel();
        });

        using IHost host = hostBuilder.Build();
        host.Start();

        var addressFeature = host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        addressFeature.Should().NotBeNull();
        addressFeature!.Addresses.Should().ContainSingle(address => address.Contains(":9090", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AddAllActuators_GenericHostWithUseCloudHosting_MiddlewareOnlyAllowsManagementPort()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.UseDefaultServiceProvider(options => options.ValidateScopes = true);
            webHostBuilder.ConfigureServices(services => services.AddRouting().AddActionDescriptorCollectionProvider());
            webHostBuilder.UseSetting("management:endpoints:port", "9090");
            webHostBuilder.UseCloudHosting();
            webHostBuilder.AddAllActuators();
            webHostBuilder.Configure(app => app.Run(async context => await context.Response.WriteAsync("Hello World!")));
            webHostBuilder.Configure(applicationBuilder => applicationBuilder.UseRouting());
            webHostBuilder.UseKestrel();
        });

        using IHost host = hostBuilder.Build();
        host.Start();

        using var httpClient = new HttpClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost:9090/actuator"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await httpClient.GetAsync(new Uri("http://localhost:8080/actuator"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")] // https://github.com/dotnet/aspnetcore/issues/42273
    public async Task AddAllActuators_GenericHost_RequiresSSL()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.UseDefaultServiceProvider(options => options.ValidateScopes = true);
            webHostBuilder.ConfigureServices(services => services.AddRouting().AddActionDescriptorCollectionProvider());
            webHostBuilder.UseSetting("management:endpoints:port", "9090");
            webHostBuilder.UseSetting("management:endpoints:sslenabled", "true");
            webHostBuilder.UseCloudHosting();
            webHostBuilder.AddAllActuators();
            webHostBuilder.Configure(app => app.Run(async context => await context.Response.WriteAsync("Hello World!")));
            webHostBuilder.Configure(applicationBuilder => applicationBuilder.UseRouting());
            webHostBuilder.UseKestrel();
        });

        using IHost host = hostBuilder.Build();
        host.Start();

        using var httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("https://localhost:9090/actuator"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await httpClient.GetAsync(new Uri("http://localhost:8080/actuator"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

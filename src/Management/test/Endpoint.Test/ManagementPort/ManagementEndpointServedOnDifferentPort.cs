// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Hosting;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.ManagementPort;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.ManagementPort;

public sealed class ManagementEndpointServedOnDifferentPort
{
    [Fact]
    public void AddAllActuators_WebApplication_MakeSureTheManagementPortIsSet()
    {
        ImmutableDictionary<string, string> appsettings = new Dictionary<string, string>
        {
            { "management:endpoints:port", "9090" }
        }.ToImmutableDictionary();

        WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.Configuration.AddInMemoryCollection(appsettings);
        hostBuilder.AddAllActuators();
        hostBuilder.WebHost.UseTestServer();
        hostBuilder.Services.AddActionDescriptorCollectionProvider();

        WebApplication app = hostBuilder.Build();
        app.MapGet("/", () => "Hello World!");
        app.Start();

        using HttpClient httpClient = app.GetTestServer().CreateClient();
        HttpResponseMessage response = httpClient.GetAsync(new Uri("http://localhost:9090/actuator")).Result;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = httpClient.GetAsync(new Uri("http://localhost:8080")).Result;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddAllActuators_WorksWithUseCloudHosting()
    {
        ImmutableDictionary<string, string> appsettings = new Dictionary<string, string>
        {
            { "management:endpoints:port", "9090" }
        }.ToImmutableDictionary();

        WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.Configuration.AddInMemoryCollection(appsettings);
        hostBuilder.UseCloudHosting();
        hostBuilder.AddAllActuators();
        hostBuilder.Services.AddActionDescriptorCollectionProvider();
        hostBuilder.WebHost.UseTestServer();

        WebApplication app = hostBuilder.Build();
        app.MapGet("/", () => "Hello World!");
        app.Start();

        using HttpClient httpClient = app.GetTestServer().CreateClient();
        HttpResponseMessage response = httpClient.GetAsync(new Uri("https://localhost:9090/actuator")).Result;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = httpClient.GetAsync(new Uri("http://localhost:5100")).Result;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddAllActuators_WebApplication_MakeSure_SSLEnabled()
    {
        using var scope1 = new EnvironmentVariableScope("ASPNETCORE_URLS", null);
        using var scope2 = new EnvironmentVariableScope("PORT", null);

        ImmutableDictionary<string, string> appsettings = new Dictionary<string, string>
        {
            { "management:endpoints:port", "9090" },
            { "management:endpoints:sslenabled", "true" }
        }.ToImmutableDictionary();

        WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.Configuration.AddInMemoryCollection(appsettings);
        hostBuilder.AddAllActuators();

        hostBuilder.Services.AddActionDescriptorCollectionProvider();
        hostBuilder.WebHost.UseTestServer();

        WebApplication app = hostBuilder.Build();
        app.MapGet("/", () => "Hello World!");
        app.Start();

        using HttpClient httpClient = app.GetTestServer().CreateClient();
        HttpResponseMessage response = httpClient.GetAsync(new Uri("https://localhost:9090/actuator")).Result;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = httpClient.GetAsync(new Uri("http://localhost:8080")).Result;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddAllActuators_GenericHost_MakeSureTheManagementPortIsSet()
    {
        ImmutableDictionary<string, string> settings = new Dictionary<string, string>
        {
            { "management:endpoints:port", "9090" },
            { "management:endpoints:sslenabled", "true" }
        }.ToImmutableDictionary();

        IHostBuilder hostBuilder = new HostBuilder().ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(settings)).ConfigureWebHost(
            webHostBuilder =>
            {
                webHostBuilder.Configure(app => app.UseRouting());
                webHostBuilder.ConfigureServices(svc => svc.AddRouting());
                webHostBuilder.UseSetting("management:endpoints:port", "9090");
                webHostBuilder.ConfigureServices(svc => svc.AddActionDescriptorCollectionProvider());
                webHostBuilder.AddAllActuators();

                webHostBuilder.UseTestServer().ConfigureServices(services => services.AddRouting())
                    .Configure(applicationBuilder => applicationBuilder.UseRouting());
            });

        using IHost host = hostBuilder.Build();

        host.Start();

        using HttpClient httpClient = host.GetTestServer().CreateClient();
        HttpResponseMessage response = httpClient.GetAsync(new Uri("http://localhost:9090/actuator")).Result;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddAllActuators_GenericHost_MakeSure_SSLEnabled()
    {
        using var scope1 = new EnvironmentVariableScope("ASPNETCORE_URLS", null);
        using var scope2 = new EnvironmentVariableScope("PORT", null);

        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.Configure(app => app.UseRouting().Run(async context => await context.Response.WriteAsync("Response from Run Middleware")));
            webHostBuilder.ConfigureServices(svc => svc.AddRouting());
            webHostBuilder.ConfigureServices(svc => svc.AddActionDescriptorCollectionProvider());
            webHostBuilder.UseSetting("management:endpoints:port", "9090");
            webHostBuilder.UseSetting("management:endpoints:sslenabled", "true");

            webHostBuilder.UseTestServer().ConfigureServices(services => services.AddRouting())
                .Configure(applicationBuilder => applicationBuilder.UseRouting());

            webHostBuilder.AddAllActuators();
        });

        using IHost host = hostBuilder.Build();

        host.Start();
        using HttpClient httpClient = host.GetTestServer().CreateClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("https://localhost:9090/actuator"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        response = await httpClient.GetAsync(new Uri("http://localhost:8080/actuator"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var jsonResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("Not Found", jsonResponse?.Error);
        Assert.Equal("Path not found at port", jsonResponse?.Message);
    }
}

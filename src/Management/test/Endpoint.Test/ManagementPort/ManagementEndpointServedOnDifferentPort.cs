// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Hosting;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test;

public class ManagementEndpointServedOnDifferentPort
{
    [Fact]
    public void AddAllActuators_WebApplication_MakeSureTheManagementPortIsSet()
    {
        ImmutableDictionary<string, string> config = new Dictionary<string, string>
        {
            { "management:endpoints:port", "9090" }
        }.ToImmutableDictionary();

        WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.Configuration.AddInMemoryCollection(config);
        hostBuilder.AddAllActuators();
        hostBuilder.WebHost.UseTestServer();

        WebApplication app = hostBuilder.Build();
        app.MapGet("/", () => "Hello World!");
        app.Start();

        HttpClient httpClient = app.GetTestServer().CreateClient();

        HttpResponseMessage response = httpClient.GetAsync(new Uri("http://localhost:9090/actuator")).Result;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = httpClient.GetAsync(new Uri("http://localhost:8080")).Result;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddAllActuators_WorksWithUseCloudHosting()
    {
        ImmutableDictionary<string, string> config = new Dictionary<string, string>
        {
            { "management:endpoints:port", "9090" }
        }.ToImmutableDictionary();

        WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.Configuration.AddInMemoryCollection(config);
        hostBuilder.UseCloudHosting();
        hostBuilder.AddAllActuators();
        hostBuilder.WebHost.UseTestServer();

        WebApplication app = hostBuilder.Build();
        app.MapGet("/", () => "Hello World!");
        app.Start();

        HttpClient httpClient = app.GetTestServer().CreateClient();
        HttpResponseMessage response = httpClient.GetAsync(new Uri("https://localhost:9090/actuator")).Result;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = httpClient.GetAsync(new Uri("http://localhost:5100")).Result;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddAllActuators_WebApplication_MakeSure_SSLEnabled()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
        Environment.SetEnvironmentVariable("PORT", null);

        ImmutableDictionary<string, string> config = new Dictionary<string, string>
        {
            { "management:endpoints:port", "9090" },
            { "management:endpoints:sslenabled", "true" }
        }.ToImmutableDictionary();

        WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.Configuration.AddInMemoryCollection(config);
        hostBuilder.AddAllActuators();
        hostBuilder.WebHost.UseTestServer();

        WebApplication app = hostBuilder.Build();
        app.MapGet("/", () => "Hello World!");
        app.Start();

        HttpClient httpClient = app.GetTestServer().CreateClient();
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

        IHostBuilder hostBuilder = new HostBuilder().ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(settings)).ConfigureWebHost(
            webhostBuilder =>
            {
                webhostBuilder.Configure(app => app.UseRouting());
                webhostBuilder.ConfigureServices(svc => svc.AddRouting());
                webhostBuilder.UseSetting("management:endpoints:port", "9090");
                webhostBuilder.AddAllActuators();
                webhostBuilder.UseTestServer().ConfigureServices(s => s.AddRouting()).Configure(a => a.UseRouting());
            });

        using IHost host = hostBuilder.Build();

        host.Start();

        HttpClient httpClient = host.GetTestServer().CreateClient();
        HttpResponseMessage response = httpClient.GetAsync(new Uri("http://localhost:9090/actuator")).Result;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddAllActuators_GenericHost_MakeSure_SSLEnabled()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
        Environment.SetEnvironmentVariable("PORT", null);

        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(webhostBuilder =>
        {
            webhostBuilder.Configure(app => app.UseRouting().Run(async context => await context.Response.WriteAsync("Response from Run Middleware")));
            webhostBuilder.ConfigureServices(svc => svc.AddRouting());
            webhostBuilder.UseSetting("management:endpoints:port", "9090");
            webhostBuilder.UseSetting("management:endpoints:sslenabled", "true");
            webhostBuilder.UseTestServer().ConfigureServices(s => s.AddRouting()).Configure(a => a.UseRouting());

            webhostBuilder.AddAllActuators();
        });

        using IHost host = hostBuilder.Build();

        host.Start();
        HttpClient httpClient = host.GetTestServer().CreateClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("https://localhost:9090/actuator"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        response = await httpClient.GetAsync(new Uri("http://localhost:8080/actuator"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

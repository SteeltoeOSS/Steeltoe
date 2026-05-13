// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Hosting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test;

public class ManagementEndpointServedOnDifferentPort
{
    private class LocalPortStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.Use(async (context, nextMiddleware) =>
                {
                    if (context.Connection.LocalPort == 0)
                    {
                        context.Connection.LocalPort = context.Request.Host.Port ?? 80;
                    }

                    await nextMiddleware();
                });

                next(app);
            };
        }
    }

    [Fact]
    public void AddAllActuators_WebApplication_MakeSureTheManagementPortIsSet()
    {
        ImmutableDictionary<string, string> config = new Dictionary<string, string>
        {
            { "management:endpoints:port", "9090" }
        }.ToImmutableDictionary();

        WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.Services.AddSingleton<IStartupFilter, LocalPortStartupFilter>();
        hostBuilder.Configuration.AddInMemoryCollection(config);
        hostBuilder.AddAllActuators();
        hostBuilder.WebHost.UseTestServer();

        WebApplication app = hostBuilder.Build();
        app.MapGet("/", () => "Hello World!");
        app.Start();

        HttpClient httpClient = app.GetTestServer().CreateClient();
        HttpResponseMessage response = httpClient.GetAsync("http://localhost:9090/actuator").Result;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = httpClient.GetAsync("http://localhost:8080").Result;
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
        hostBuilder.Services.AddSingleton<IStartupFilter, LocalPortStartupFilter>();
        hostBuilder.Configuration.AddInMemoryCollection(config);
        hostBuilder.UseCloudHosting(5100);
        hostBuilder.AddAllActuators();
        hostBuilder.WebHost.UseTestServer();

        WebApplication app = hostBuilder.Build();
        app.MapGet("/", () => "Hello World!");
        app.Start();

        HttpClient httpClient = app.GetTestServer().CreateClient();
        HttpResponseMessage response = httpClient.GetAsync("https://localhost:9090/actuator").Result;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = httpClient.GetAsync("http://localhost:5100").Result;
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
        hostBuilder.Services.AddSingleton<IStartupFilter, LocalPortStartupFilter>();
        hostBuilder.Configuration.AddInMemoryCollection(config);
        hostBuilder.AddAllActuators();
        hostBuilder.WebHost.UseTestServer();

        WebApplication app = hostBuilder.Build();
        app.MapGet("/", () => "Hello World!");
        app.Start();

        HttpClient httpClient = app.GetTestServer().CreateClient();
        HttpResponseMessage response = httpClient.GetAsync("https://localhost:9090/actuator").Result;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = httpClient.GetAsync("http://localhost:8080").Result;
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
                webhostBuilder.ConfigureServices(svc => svc.AddSingleton<IStartupFilter, LocalPortStartupFilter>());
                webhostBuilder.Configure(app => app.UseRouting());
                webhostBuilder.ConfigureServices(svc => svc.AddRouting());
                webhostBuilder.UseSetting("management:endpoints:port", "9090");
                webhostBuilder.AddAllActuators();
                webhostBuilder.UseTestServer().ConfigureServices(s => s.AddRouting()).Configure(a => a.UseRouting());
            });

        using IHost host = hostBuilder.Build();

        host.Start();

        HttpClient httpClient = host.GetTestServer().CreateClient();
        HttpResponseMessage response = httpClient.GetAsync("http://localhost:9090/actuator").Result;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddAllActuators_GenericHost_MakeSure_SSLEnabled()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
        Environment.SetEnvironmentVariable("PORT", null);

        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(webhostBuilder =>
        {
            webhostBuilder.ConfigureServices(svc => svc.AddSingleton<IStartupFilter, LocalPortStartupFilter>());
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
        HttpResponseMessage response = await httpClient.GetAsync("https://localhost:9090/actuator");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        response = await httpClient.GetAsync("http://localhost:8080/actuator");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddAllActuators_WebApplication_IgnoresSpoofedHostHeader()
    {
        var appSettings = new Dictionary<string, string>
        {
            ["URLS"] = "http://localhost:5000", ["management:endpoints:port"] = "9090",
        };

        var hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.Configuration.AddInMemoryCollection(appSettings);
        hostBuilder.AddAllActuators();
        hostBuilder.WebHost.UseKestrel();

        await using var app = hostBuilder.Build();
        app.MapGet("/", () => "Hello World!");
        await app.StartAsync();

        // ReSharper disable once ShortLivedHttpClient
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("http://localhost:5000");

        var helloResponse = await httpClient.GetAsync("/");
        helloResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var goodActuatorResponse = await httpClient.GetAsync("http://localhost:9090/actuator");
        goodActuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var badActuatorResponse = await httpClient.GetAsync("/actuator");
        badActuatorResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var spoofRequest = new HttpRequestMessage(HttpMethod.Get, "/actuator");
        spoofRequest.Headers.Host = $"anything:{9090}";

        var spoofResponse = await httpClient.SendAsync(spoofRequest);
        spoofResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
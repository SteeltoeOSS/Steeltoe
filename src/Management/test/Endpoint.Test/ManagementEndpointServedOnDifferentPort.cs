// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Hosting;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test;

public class ManagementEndpointServedOnDifferentPort
{
    public ManagementEndpointServedOnDifferentPort()
    {
        ClearEnvVars();
    }

    [Fact]
    public void AddAllActuators_WebApplication_MakeSureTheManagementPortIsSet()
    {
        ImmutableDictionary<string, string> config = new Dictionary<string, string>
        {
            { "management:endpoints:port", "9090" }
        }.ToImmutableDictionary();

        WebApplicationBuilder hostBuilder = TestHelpers.GetTestWebApplicationBuilder();
        hostBuilder.Configuration.AddInMemoryCollection(config);
        hostBuilder.AddAllActuators();

        string settings = hostBuilder.WebHost.GetSetting(WebHostDefaults.ServerUrlsKey);
        Assert.Contains("http://*:9090", settings);
        ClearEnvVars();
    }

    [Fact]
    public void AddAllActuators_WorksWithUseCloudHosting()
    {
        ImmutableDictionary<string, string> config = new Dictionary<string, string>
        {
            { "management:endpoints:port", "9090" }
        }.ToImmutableDictionary();

        WebApplicationBuilder hostBuilder = TestHelpers.GetTestWebApplicationBuilder();
        hostBuilder.Configuration.AddInMemoryCollection(config);
        hostBuilder.UseCloudHosting(5100);
        hostBuilder.AddAllActuators();

        string settings = hostBuilder.WebHost.GetSetting(WebHostDefaults.ServerUrlsKey);
        Assert.Contains("http://*:9090", settings);
        Assert.Contains("http://*:5100", settings);
        ClearEnvVars();
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

        WebApplicationBuilder hostBuilder = TestHelpers.GetTestWebApplicationBuilder();
        hostBuilder.Configuration.AddInMemoryCollection(config);
        hostBuilder.AddAllActuators();

        string settings = hostBuilder.WebHost.GetSetting(WebHostDefaults.ServerUrlsKey);
        Assert.Contains("https://*:9090", settings);
        ClearEnvVars();
    }

    [Fact]
    public void AddAllActuators_GenericHost_MakeSureTheManagementPortIsSet()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
        Environment.SetEnvironmentVariable("PORT", null);

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
                webhostBuilder.UseKestrel();
                webhostBuilder.AddAllActuators();
            });

        using IHost host = hostBuilder.Build();

        host.Start();

        var httpClient = new HttpClient();
        HttpResponseMessage response = httpClient.GetAsync("http://localhost:9090/actuator").Result;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ClearEnvVars();
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
            webhostBuilder.UseKestrel();
            webhostBuilder.AddAllActuators();
        });

        using IHost host = hostBuilder.Build();

        host.Start();

        var handler = new HttpClientHandler();
        handler.ClientCertificateOptions = ClientCertificateOption.Manual;

        handler.ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) =>
        {
            return true;
        };

        var httpClient = new HttpClient(handler);
        await Assert.ThrowsAsync<HttpRequestException>(() => httpClient.GetAsync("http://localhost:9090/actuator"));

        HttpResponseMessage response = await httpClient.GetAsync("https://localhost:9090/actuator");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        response = await httpClient.GetAsync("http://localhost:8080/actuator");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        response = await httpClient.GetAsync("http://localhost:8080");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ClearEnvVars();
    }

    private void ClearEnvVars()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
        Environment.SetEnvironmentVariable("PORT", null);
    }
}

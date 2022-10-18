// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.

using System.Collections.Immutable;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute.Extensions;
using OpenTelemetry.Trace;
using Steeltoe.Common;
using Steeltoe.Common.Availability;
using Steeltoe.Common.Hosting;
using Steeltoe.Extensions.Logging;
using Steeltoe.Extensions.Logging.DynamicSerilog;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.DbMigrations;
using Steeltoe.Management.Endpoint.Env;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Health.Test;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Info.Contributor;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Mappings;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Refresh;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;
using Steeltoe.Management.Info;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test;

        
public class ManagementEndpointServedOnDifferentPort : BaseTest
{

    [Fact]
    public void AddAllActuators_WebApplication_MakeSureTheManagementPortIsSet()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
        Environment.SetEnvironmentVariable("PORT", null);
        var config = new Dictionary<string, string>
        {
            { "management:endpoints:port", "9090" },
           
        }.ToImmutableDictionary();

        WebApplicationBuilder hostBuilder = TestHelpers.GetTestWebApplicationBuilder();
        hostBuilder.Configuration.AddInMemoryCollection(config);
        hostBuilder.AddAllActuators();
        

        string settings = hostBuilder.WebHost.GetSetting(WebHostDefaults.ServerUrlsKey);
        Assert.Contains("http://*:9090", settings);
    }

    [Fact]
    public void AddAllActuators_WebApplication_MakeSure_SSLEnabled()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
        Environment.SetEnvironmentVariable("PORT", null);
        var config = new Dictionary<string, string>
        {
            { "management:endpoints:port", "9090" },
            { "management:endpoints:sslenabled", "true" },

        }.ToImmutableDictionary();

        WebApplicationBuilder hostBuilder = TestHelpers.GetTestWebApplicationBuilder();
        hostBuilder.Configuration.AddInMemoryCollection(config);
        hostBuilder.AddAllActuators();


        string settings = hostBuilder.WebHost.GetSetting(WebHostDefaults.ServerUrlsKey);
        Assert.Contains("https://*:9090", settings);
    }
    [Fact]
    public void AddAllActuators_GenericHost_MakeSureTheManagementPortIsSet()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
        Environment.SetEnvironmentVariable("PORT", null);
        var settings = new Dictionary<string, string>
        {
            { "management:endpoints:port", "9090" },
            { "management:endpoints:sslenabled", "true" },

        }.ToImmutableDictionary();
        IHostBuilder hostBuilder = new HostBuilder()
            .ConfigureAppConfiguration(
            cbuilder => cbuilder.AddInMemoryCollection(settings))
        .ConfigureWebHost(webhostBuilder =>
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
        var response = httpClient.GetAsync("http://localhost:9090/actuator").Result;
        Assert.Equal( HttpStatusCode.OK, response.StatusCode);

    }

    [Fact]
    public async Task AddAllActuators_GenericHost_MakeSure_SSLEnabled()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
        Environment.SetEnvironmentVariable("PORT", null);

        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(webhostBuilder =>
        {
            webhostBuilder.Configure(app => app.UseRouting());
            webhostBuilder.ConfigureServices(svc => svc.AddRouting());
            webhostBuilder.UseSetting("management:endpoints:port", "9090");
            webhostBuilder.UseSetting("management:endpoints:sslenabled", "true");
            webhostBuilder.UseKestrel();
            webhostBuilder.AddAllActuators();
        });

        using IHost host = hostBuilder.Build();

        host.Start();

        var httpClient = new HttpClient();

        await Assert.ThrowsAsync<HttpRequestException>(() => httpClient.GetAsync("http://localhost:9090/actuator"));

        var response = await httpClient.GetAsync("https://localhost:9090/actuator");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);


    }
}

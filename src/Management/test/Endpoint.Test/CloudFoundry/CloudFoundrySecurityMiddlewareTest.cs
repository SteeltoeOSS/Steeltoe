// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Options;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.CloudFoundry;

public sealed class CloudFoundrySecurityMiddlewareTest : BaseTest
{
    private readonly EnvironmentVariableScope _scope = new("VCAP_APPLICATION", "some");

    [Fact]
    public async Task CloudFoundrySecurityMiddleware_ReturnsServiceUnavailable()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "true",
            ["management:endpoints:path"] = "/",
            ["management:endpoints:info:enabled"] = "true",
            ["info:application:name"] = "foobar",
            ["info:application:version"] = "1.0.0",
            ["info:application:date"] = "5/1/2008",
            ["info:application:time"] = "8:30:52 AM",
            ["info:NET:type"] = "Core",
            ["info:NET:version"] = "2.0.0",
            ["info:NET:ASPNET:type"] = "Core",
            ["info:NET:ASPNET:version"] = "2.0.0"
        };

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<StartupWithSecurity>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        // Application Id Missing
        using (var server = new TestServer(builder))
        {
            HttpClient client = server.CreateClient();
            HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }

        var appSettings2 = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "true",
            ["management:endpoints:path"] = "/",
            ["management:endpoints:info:enabled"] = "true",
            ["info:application:name"] = "foobar",
            ["info:application:version"] = "1.0.0",
            ["info:application:date"] = "5/1/2008",
            ["info:application:time"] = "8:30:52 AM",
            ["info:NET:type"] = "Core",
            ["info:NET:version"] = "2.0.0",
            ["info:NET:ASPNET:type"] = "Core",
            ["info:NET:ASPNET:version"] = "2.0.0",
            ["vcap:application:application_id"] = "foobar"
        };

        IWebHostBuilder builder2 = new WebHostBuilder().UseStartup<StartupWithSecurity>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings2));

        // CloudFoundry Api missing
        using (var server = new TestServer(builder2))
        {
            HttpClient client = server.CreateClient();
            HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }

        var appSettings3 = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "true",
            ["management:endpoints:path"] = "/",
            ["management:endpoints:info:enabled"] = "true",
            ["info:application:name"] = "foobar",
            ["info:application:version"] = "1.0.0",
            ["info:application:date"] = "5/1/2008",
            ["info:application:time"] = "8:30:52 AM",
            ["info:NET:type"] = "Core",
            ["info:NET:version"] = "2.0.0",
            ["info:NET:ASPNET:type"] = "Core",
            ["info:NET:ASPNET:version"] = "2.0.0",
            ["vcap:application:application_id"] = "foobar",
            ["vcap:application:cf_api"] = "http://localhost:9999/foo"
        };

        IWebHostBuilder builder3 = new WebHostBuilder().UseStartup<StartupWithSecurity>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings3));

        // Endpoint not configured
        using (var server = new TestServer(builder3))
        {
            HttpClient client = server.CreateClient();
            HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/barfoo"));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }

        var appSettings4 = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "true",
            ["management:endpoints:path"] = "/",
            ["management:endpoints:info:enabled"] = "true",
            ["info:application:name"] = "foobar",
            ["info:application:version"] = "1.0.0",
            ["info:application:date"] = "5/1/2008",
            ["info:application:time"] = "8:30:52 AM",
            ["info:NET:type"] = "Core",
            ["info:NET:version"] = "2.0.0",
            ["info:NET:ASPNET:type"] = "Core",
            ["info:NET:ASPNET:version"] = "2.0.0",
            ["vcap:application:application_id"] = "foobar",
            ["vcap:application:cf_api"] = "http://localhost:9999/foo"
        };

        IWebHostBuilder builder4 = new WebHostBuilder().UseStartup<StartupWithSecurity>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings4));

        using (var server = new TestServer(builder4))
        {
            HttpClient client = server.CreateClient();
            HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Fact]
    public async Task CloudFoundrySecurityMiddleware_ReturnsWithStatusOverride()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "true",
            ["management:endpoints:path"] = "/",
            ["management:endpoints:info:enabled"] = "true",
            ["management:endpoints:UseStatusCodeFromResponse"] = "false",
            ["info:application:name"] = "foobar",
            ["info:application:version"] = "1.0.0",
            ["info:application:date"] = "5/1/2008",
            ["info:application:time"] = "8:30:52 AM",
            ["info:NET:type"] = "Core",
            ["info:NET:version"] = "2.0.0",
            ["info:NET:ASPNET:type"] = "Core",
            ["info:NET:ASPNET:version"] = "2.0.0"
        };

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<StartupWithSecurity>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using (var server = new TestServer(builder))
        {
            HttpClient client = server.CreateClient();
            HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var appSettings3 = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "true",
            ["management:endpoints:path"] = "/",
            ["management:endpoints:info:enabled"] = "true",
            ["management:endpoints:UseStatusCodeFromResponse"] = "false",
            ["info:application:name"] = "foobar",
            ["info:application:version"] = "1.0.0",
            ["info:application:date"] = "5/1/2008",
            ["info:application:time"] = "8:30:52 AM",
            ["info:NET:type"] = "Core",
            ["info:NET:version"] = "2.0.0",
            ["info:NET:ASPNET:type"] = "Core",
            ["info:NET:ASPNET:version"] = "2.0.0",
            ["vcap:application:application_id"] = "foobar",
            ["vcap:application:cf_api"] = "http://localhost:9999/foo"
        };

        IWebHostBuilder builder3 = new WebHostBuilder().UseStartup<StartupWithSecurity>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings3));

        using (var server = new TestServer(builder3))
        {
            HttpClient client = server.CreateClient();
            HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Fact]
    public async Task CloudFoundrySecurityMiddleware_ReturnsSecurityException()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "true",

            ["management:endpoints:path"] = "/",
            ["management:endpoints:info:enabled"] = "true",

            ["info:application:name"] = "foobar",
            ["info:application:version"] = "1.0.0",
            ["info:application:date"] = "5/1/2008",
            ["info:application:time"] = "8:30:52 AM",
            ["info:NET:type"] = "Core",
            ["info:NET:version"] = "2.0.0",
            ["info:NET:ASPNET:type"] = "Core",
            ["info:NET:ASPNET:version"] = "2.0.0",
            ["vcap:application:application_id"] = "foobar",
            ["vcap:application:cf_api"] = "http://localhost:9999/foo"
        };

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<StartupWithSecurity>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CloudFoundrySecurityMiddleware_SkipsSecurityCheckIfEnabledFalse()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "true",

            ["management:endpoints:path"] = "/",
            ["management:endpoints:info:enabled"] = "true",

            ["management:endpoints:cloudfoundry:enabled"] = "false",
            ["info:application:name"] = "foobar",
            ["info:application:version"] = "1.0.0",
            ["info:application:date"] = "5/1/2008",
            ["info:application:time"] = "8:30:52 AM",
            ["info:NET:type"] = "Core",
            ["info:NET:version"] = "2.0.0",
            ["info:NET:ASPNET:type"] = "Core",
            ["info:NET:ASPNET:version"] = "2.0.0",
            ["vcap:application:application_id"] = "foobar",
            ["vcap:application:cf_api"] = "http://localhost:9999/foo"
        };

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<StartupWithSecurity>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/info"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CloudFoundrySecurityMiddleware_SkipsSecurityCheckIfEnabledFalseViaEnvironmentVariables()
    {
        using var scope = new EnvironmentVariableScope("MANAGEMENT__ENDPOINTS__CLOUDFOUNDRY__ENABLED", "False");

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "true",
            ["management:endpoints:path"] = "/",
            ["management:endpoints:info:enabled"] = "true",
            ["info:application:name"] = "foobar",
            ["info:application:version"] = "1.0.0",
            ["info:application:date"] = "5/1/2008",
            ["info:application:time"] = "8:30:52 AM",
            ["info:NET:type"] = "Core",
            ["info:NET:version"] = "2.0.0",
            ["info:NET:ASPNET:type"] = "Core",
            ["info:NET:ASPNET:version"] = "2.0.0",
            ["vcap:application:application_id"] = "foobar",
            ["vcap:application:cf_api"] = "http://localhost:9999/foo"
        };

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<StartupWithSecurity>().ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(appSettings);
            configuration.AddEnvironmentVariables();
        });

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CloudFoundrySecurityMiddleware_InvokeAsync_ReturnsExpected()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "true",

            ["management:endpoints:info:enabled"] = "true",

            ["info:application:name"] = "foobar",
            ["info:application:version"] = "1.0.0",
            ["info:application:date"] = "5/1/2008",
            ["info:application:time"] = "8:30:52 AM",
            ["info:NET:type"] = "Core",
            ["info:NET:version"] = "2.0.0",
            ["info:NET:ASPNET:type"] = "Core",
            ["info:NET:ASPNET:version"] = "2.0.0",
            ["vcap:application:application_id"] = "foobar",
            ["vcap:application:cf_api"] = "http://localhost:9999/foo"
        };

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<StartupWithSecurity>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); // We expect the authorization to fail, but the FindEndpoint logic to work.

        HttpResponseMessage response2 = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"));
        Assert.Equal(HttpStatusCode.Unauthorized, response2.StatusCode);
    }

    [Fact]
    public void GetAccessToken_ReturnsExpected()
    {
        IOptionsMonitor<CloudFoundryEndpointOptions> endpointOptionsMonitor = GetOptionsMonitorFromSettings<CloudFoundryEndpointOptions>();
        IOptionsMonitor<ManagementOptions> managementOptionsMonitor = GetOptionsMonitorFromSettings<ManagementOptions>();

        var middleware = new CloudFoundrySecurityMiddleware(managementOptionsMonitor, endpointOptionsMonitor, Enumerable.Empty<EndpointOptions>(), null,
            NullLoggerFactory.Instance);

        HttpContext context1 = CreateRequest("GET", "/");
        string token = middleware.GetAccessToken(context1.Request);
        Assert.Empty(token);

        HttpContext context2 = CreateRequest("GET", "/");
        context2.Request.Headers.Append("Authorization", new StringValues("Bearer foobar"));
        string token2 = middleware.GetAccessToken(context2.Request);
        Assert.Equal("foobar", token2);
    }

    [Fact]
    public async Task GetPermissions_ReturnsExpected()
    {
        IOptionsMonitor<CloudFoundryEndpointOptions> endpointOptionsMonitor = GetOptionsMonitorFromSettings<CloudFoundryEndpointOptions>();
        IOptionsMonitor<ManagementOptions> managementOptionsMonitor = GetOptionsMonitorFromSettings<ManagementOptions>();

        var middleware = new CloudFoundrySecurityMiddleware(managementOptionsMonitor, endpointOptionsMonitor, Enumerable.Empty<EndpointOptions>(), null,
            NullLoggerFactory.Instance);

        HttpContext context = CreateRequest("GET", "/");
        SecurityResult result = await middleware.GetPermissionsAsync(context);
        Assert.NotNull(result);
        Assert.Equal(Permissions.None, result.Permissions);
        Assert.Equal(HttpStatusCode.Unauthorized, result.Code);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope.Dispose();
        }

        base.Dispose(disposing);
    }

    private HttpContext CreateRequest(string method, string path)
    {
        HttpContext context = new DefaultHttpContext
        {
            TraceIdentifier = Guid.NewGuid().ToString()
        };

        context.Response.Body = new MemoryStream();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost");
        return context;
    }
}

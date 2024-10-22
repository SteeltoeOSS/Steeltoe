// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test.Actuators.CloudFoundry;

public sealed class CloudFoundrySecurityMiddlewareTest : BaseTest
{
    private readonly EnvironmentVariableScope _scope = new("VCAP_APPLICATION", "some");

    [Fact]
    public async Task CloudFoundrySecurityMiddleware_MissingApplicationID_ReturnsServiceUnavailable()
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

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<StartupWithSecurity>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using IWebHost host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task CloudFoundrySecurityMiddleware_MissingCloudFoundryApi_ReturnsServiceUnavailable()
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
            ["vcap:application:application_id"] = "foobar"
        };

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<StartupWithSecurity>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using IWebHost host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task CloudFoundrySecurityMiddleware_EndpointNotConfigured_ReturnsServiceUnavailable()
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

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<StartupWithSecurity>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using IWebHost host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/barfoo"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task CloudFoundrySecurityMiddleware_MissingAccessToken_ReturnsUnauthorized()
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

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<StartupWithSecurity>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using IWebHost host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CloudFoundrySecurityMiddleware_UseStatusCodeFromResponseFalse_ReturnsOk()
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

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<StartupWithSecurity>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using IWebHost host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CloudFoundrySecurityMiddleware_UseStatusCodeFromResponseFalse_ReturnsUnauthorized()
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
            ["info:NET:ASPNET:version"] = "2.0.0",
            ["vcap:application:application_id"] = "foobar",
            ["vcap:application:cf_api"] = "http://localhost:9999/foo"
        };

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<StartupWithSecurity>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using IWebHost host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
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

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<StartupWithSecurity>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using IWebHost host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
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

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<StartupWithSecurity>();

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(appSettings);
            configuration.AddEnvironmentVariables();
        });

        using IWebHost host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
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

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<StartupWithSecurity>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using IWebHost host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); // We expect the authorization to fail, but the FindEndpoint logic to work.

        HttpResponseMessage response2 = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"));
        Assert.Equal(HttpStatusCode.Unauthorized, response2.StatusCode);
    }

    [Fact]
    public async Task GetAccessToken_ReturnsExpected()
    {
        IOptionsMonitor<CloudFoundryEndpointOptions> endpointOptionsMonitor = GetOptionsMonitorFromSettings<CloudFoundryEndpointOptions>();
        IOptionsMonitor<ManagementOptions> managementOptionsMonitor = GetOptionsMonitorFromSettings<ManagementOptions>();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddCloudFoundryActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var permissionsProvider = serviceProvider.GetRequiredService<PermissionsProvider>();

        var middleware = new CloudFoundrySecurityMiddleware(managementOptionsMonitor, endpointOptionsMonitor, [], permissionsProvider,
            NullLogger<CloudFoundrySecurityMiddleware>.Instance, null);

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

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddCloudFoundryActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var permissionsProvider = serviceProvider.GetRequiredService<PermissionsProvider>();

        var middleware = new CloudFoundrySecurityMiddleware(managementOptionsMonitor, endpointOptionsMonitor, [], permissionsProvider,
            NullLogger<CloudFoundrySecurityMiddleware>.Instance, null);

        HttpContext context = CreateRequest("GET", "/");
        SecurityResult result = await middleware.GetPermissionsAsync(context);
        Assert.NotNull(result);
        Assert.Equal(EndpointPermissions.None, result.Permissions);
        Assert.Equal(HttpStatusCode.Unauthorized, result.Code);
    }

    [Fact]
    public async Task Throws_when_Add_method_not_called()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        await using WebApplication app = builder.Build();

        Action action = () => app.UseCloudFoundrySecurity();
        action.Should().ThrowExactly<InvalidOperationException>().WithMessage("Please call IServiceCollection.AddCloudFoundryActuator first.");
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

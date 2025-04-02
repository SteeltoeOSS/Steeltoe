// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test.Actuators.CloudFoundry;

public sealed class CloudFoundrySecurityMiddlewareTest : BaseTest
{
    private readonly EnvironmentVariableScope _scope = new("VCAP_APPLICATION", "{}");

    [Fact]
    public async Task CloudFoundrySecurityMiddleware_MissingApplicationID_ReturnsServiceUnavailable()
    {
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<StartupWithSecurity>();

        using IWebHost host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("""{"security_error":"Application ID is not available"}""", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
    }

    [Fact]
    public async Task CloudFoundrySecurityMiddleware_MissingCloudFoundryApi_ReturnsServiceUnavailable()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["vcap:application:application_id"] = "foobar"
        };

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<StartupWithSecurity>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using IWebHost host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("""{"security_error":"Cloud controller URL is not available"}""", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
    }

    [Fact]
    public async Task CloudFoundrySecurityMiddleware_TargetEndpointNotConfigured_DelegatesToEndpointMiddleware()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["vcap:application:application_id"] = "foobar",
            ["vcap:application:cf_api"] = "http://localhost:9999/foo"
        };

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<StartupWithSecurity>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using IWebHost host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/does-not-exist"), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Null(response.Content.Headers.ContentType);
    }

    [Fact]
    public async Task CloudFoundrySecurityMiddleware_MissingAccessToken_ReturnsUnauthorized()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["vcap:application:application_id"] = "foobar",
            ["vcap:application:cf_api"] = "http://localhost:9999/foo"
        };

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<StartupWithSecurity>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using IWebHost host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("""{"security_error":"Authorization header is missing or invalid"}""", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
    }

    [Fact]
    public async Task CloudFoundrySecurityMiddleware_UseStatusCodeFromResponseFalse_ReturnsOkAndContent()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:UseStatusCodeFromResponse"] = "false"
        };

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<StartupWithSecurity>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using IWebHost host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("""{"security_error":"Application ID is not available"}""", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
    }

    [Fact]
    public async Task CloudFoundrySecurityMiddleware_UseStatusCodeFromResponseFalse_ReturnsUnauthorized()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:UseStatusCodeFromResponse"] = "false",
            ["vcap:application:application_id"] = "foobar",
            ["vcap:application:cf_api"] = "http://localhost:9999/foo"
        };

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<StartupWithSecurity>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using IWebHost host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("""{"security_error":"Authorization header is missing or invalid"}""", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
    }

    [Fact]
    public async Task CloudFoundrySecurityMiddleware_SkipsSecurityCheckIfCloudFoundryDisabled()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:CloudFoundry:Enabled"] = "false"
        };

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<StartupWithSecurity>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));
        using IWebHost host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseBody.Should().BeEmpty();
    }

    [Fact]
    public async Task CloudFoundrySecurityMiddleware_SkipsSecurityCheckIfCloudFoundryActuatorDisabled()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:cloudfoundry:enabled"] = "false"
        };

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<StartupWithSecurity>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using IWebHost host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.NotEqual("application/json", response.Content.Headers.ContentType.MediaType);
    }

    [Fact]
    public async Task CloudFoundrySecurityMiddleware_SkipsSecurityCheckIfCloudFoundryActuatorDisabledViaEnvironmentVariable()
    {
        using var scope = new EnvironmentVariableScope("MANAGEMENT__ENDPOINTS__CLOUDFOUNDRY__ENABLED", "False");

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<StartupWithSecurity>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddEnvironmentVariables());

        using IWebHost host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.NotEqual("application/json", response.Content.Headers.ContentType.MediaType);
    }

    [Fact]
    public async Task CloudFoundrySecurityMiddleware_InvokeAsync_ReturnsExpected()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:info:enabled"] = "true",
            ["vcap:application:application_id"] = "foobar",
            ["vcap:application:cf_api"] = "http://localhost:9999/foo"
        };

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<StartupWithSecurity>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using IWebHost host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication"), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); // We expect the authorization to fail, but the FindTargetEndpoint logic to work.
        Assert.Equal("""{"security_error":"Authorization header is missing or invalid"}""", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);

        HttpResponseMessage response2 = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response2.StatusCode);
        Assert.Equal("""{"security_error":"Authorization header is missing or invalid"}""", await response2.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.NotNull(response2.Content.Headers.ContentType);
        Assert.Equal("application/json", response2.Content.Headers.ContentType.MediaType);
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

    [Fact]
    public async Task Redacts_HTTP_headers()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["vcap:application:application_id"] = "foobar",
            ["vcap:application:cf_api"] = "http://domain-name-that-does-not-exist.com:9999/foo"
        };

        var capturingLoggerProvider = new CapturingLoggerProvider(category => category.StartsWith("System.Net.Http.HttpClient", StringComparison.Ordinal));

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Configuration.AddCloudFoundry();
        builder.Services.AddLogging(options => options.SetMinimumLevel(LogLevel.Trace).AddProvider(capturingLoggerProvider));
        builder.Services.AddCloudFoundryActuator();
        await using WebApplication app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient httpClient = app.GetTestClient();
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("http://localhost/cloudfoundryapplication"));
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "some");

        _ = await httpClient.SendAsync(requestMessage, TestContext.Current.CancellationToken);

        string logMessages = string.Join(System.Environment.NewLine, capturingLoggerProvider.GetAll());
        logMessages.Should().Contain("Authorization: *");
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

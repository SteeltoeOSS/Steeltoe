// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
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
using Steeltoe.Common.Http.HttpClientPooling;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Endpoint.Actuators.Info;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test.Actuators.CloudFoundry;

public sealed class CloudFoundrySecurityMiddlewareTest : BaseTest
{
    private static readonly string MockAccessToken =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJ0b3B0YWwuY29tIiwiZXhwIjoxNDI2NDIwODAwLCJhd2Vzb21lIjp0cnVlfQ." +
        Convert.ToBase64String("signature"u8.ToArray());

    private readonly EnvironmentVariableScope _scope = new("VCAP_APPLICATION", "{}");

    [Fact]
    public async Task MissingApplicationIdReturnsServiceUnavailable()
    {
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<StartupWithSecurity>();

        using IWebHost host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        Assert.Equal("""{"security_error":"Application ID is not available"}""",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
    }

    [Fact]
    public async Task MissingCloudFoundryApiReturnsServiceUnavailable()
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

        Assert.Equal("""{"security_error":"Cloud controller URL is not available"}""",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
    }

    [Fact]
    public async Task TargetEndpointNotConfiguredDelegatesToEndpointMiddleware()
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

        HttpResponseMessage response =
            await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/does-not-exist"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Null(response.Content.Headers.ContentType);
    }

    [Fact]
    public async Task MissingAccessTokenReturnsUnauthorized()
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

        Assert.Equal("""{"security_error":"Authorization header is missing or invalid"}""",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
    }

    [Fact]
    public async Task UseStatusCodeFromResponseFalseReturnsOkAndContent()
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

        Assert.Equal("""{"security_error":"Application ID is not available"}""",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
    }

    [Fact]
    public async Task UseStatusCodeFromResponseFalseReturnsUnauthorized()
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

        Assert.Equal("""{"security_error":"Authorization header is missing or invalid"}""",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
    }

    [Fact]
    public async Task SkipsSecurityCheckIfCloudFoundryDisabled()
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
    public async Task SkipsSecurityCheckIfCloudFoundryActuatorDisabled()
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
    public async Task SkipsSecurityCheckIfCloudFoundryActuatorDisabledViaEnvironmentVariable()
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
    public async Task GetAccessTokenReturnsExpected()
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
    public async Task GetPermissionsReturnsExpected()
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
    public async Task ThrowsWhenAddMethodNotCalled()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        await using WebApplication app = builder.Build();

        Action action = () => app.UseCloudFoundrySecurity();
        action.Should().ThrowExactly<InvalidOperationException>().WithMessage("Please call IServiceCollection.AddCloudFoundryActuator first.");
    }

    [Fact]
    public async Task RedactsHttpHeaders()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["vcap:application:application_id"] = "success",
            ["vcap:application:cf_api"] = "https://example.api.com"
        };

        var capturingLoggerProvider = new CapturingLoggerProvider(category => category.StartsWith("System.Net.Http.HttpClient", StringComparison.Ordinal));

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Configuration.AddCloudFoundry();
        builder.Services.AddLogging(options => options.SetMinimumLevel(LogLevel.Trace).AddProvider(capturingLoggerProvider));
        builder.Services.AddCloudFoundryActuator();
        await using WebApplication app = builder.Build();
        app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(CloudControllerPermissionsMock.GetHttpMessageHandler());
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient httpClient = app.GetTestClient();
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("http://localhost/cloudfoundryapplication"));
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", MockAccessToken);

        _ = await httpClient.SendAsync(requestMessage, TestContext.Current.CancellationToken);

        string logMessages = string.Join(System.Environment.NewLine, capturingLoggerProvider.GetAll());
        logMessages.Should().Contain("Authorization: *");
    }

    [ClassData(typeof(CloudFoundrySecurityMiddlewareTestScenarios))]
    [Theory]
    public async Task Returns_expected_response_on_permission_check(string scenario, HttpStatusCode? steeltoeStatusCode, string? errorMessage,
        string[] expectedLogs, bool useStatusCodeFromResponse)
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["vcap:application:application_id"] = scenario,
            ["vcap:application:cf_api"] = "https://example.api.com",
            ["management:endpoints:info:requiredPermissions"] = "FULL",
            ["management:endpoints:UseStatusCodeFromResponse"] = useStatusCodeFromResponse.ToString(CultureInfo.InvariantCulture)
        };

        using var loggerProvider = new CapturingLoggerProvider();
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.Logging.ClearProviders().AddProvider(loggerProvider);
        builder.Services.AddCloudFoundryActuator();
        builder.Services.AddHypermediaActuator();
        builder.Services.AddInfoActuator();
        builder.Configuration.AddInMemoryCollection(appSettings);

        await using WebApplication host = builder.Build();
        host.Services.GetRequiredService<HttpClientHandlerFactory>().Using(CloudControllerPermissionsMock.GetHttpMessageHandler());
        await host.StartAsync(TestContext.Current.CancellationToken);

        using var client = new HttpClient();
        var testAuthenticationRequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("http://localhost:5000/cloudfoundryapplication"));
        testAuthenticationRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", MockAccessToken);
        var testAuthorizationRequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("http://localhost:5000/cloudfoundryapplication/info"));
        testAuthorizationRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", MockAccessToken);

        HttpResponseMessage response = await client.SendAsync(testAuthenticationRequestMessage, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(steeltoeStatusCode);

        if (errorMessage != null)
        {
            string jsonErrorValue = JsonValue.Create(errorMessage).ToJsonString();
            string errorText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            errorText.Should().Be(steeltoeStatusCode == HttpStatusCode.InternalServerError ? errorMessage : $$"""{"security_error":{{jsonErrorValue}}}""");
        }
        else
        {
            string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            responseBody.Should().BeJson("""
                {
                    "type":"steeltoe",
                    "_links":{
                        "info":{
                            "href":"http://localhost:5000/cloudfoundryapplication/info",
                            "templated":false
                        },
                        "self":{
                            "href":"http://localhost:5000/cloudfoundryapplication",
                            "templated":false
                        }
                    }
                }
                """);
        }

        HttpResponseMessage fullPermissionResponse = await client.SendAsync(testAuthorizationRequestMessage, TestContext.Current.CancellationToken);
        fullPermissionResponse.StatusCode.Should().Be(scenario == "no_sensitive_data" ? HttpStatusCode.Forbidden : steeltoeStatusCode);

        string logLines = loggerProvider.GetAsText();
        logLines.Should().ContainAll(expectedLogs);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope.Dispose();
        }

        base.Dispose(disposing);
    }

    private static HttpContext CreateRequest(string method, string path)
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

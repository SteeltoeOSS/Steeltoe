// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
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
using Steeltoe.Management.Endpoint.Actuators.Info;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test.Actuators.CloudFoundry;

public sealed class CloudFoundrySecurityMiddlewareTest : IDisposable
{
    private static readonly string MockAccessToken =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJ0b3B0YWwuY29tIiwiZXhwIjoxNDI2NDIwODAwLCJhd2Vzb21lIjp0cnVlfQ." +
        Convert.ToBase64String("signature"u8.ToArray());

    private readonly EnvironmentVariableScope _scope = new("VCAP_APPLICATION", "{}");

    [Fact]
    public async Task MissingApplicationIdReturnsServiceUnavailable()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Services.AddCloudFoundryActuator();
        builder.Services.AddInfoActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient client = host.GetTestClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType.MediaType.Should().Be("application/json");

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "security_error": "Application ID is not available"
            }
            """);
    }

    [Fact]
    public async Task MissingCloudFoundryApiReturnsServiceUnavailable()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["vcap:application:application_id"] = "foobar"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddCloudFoundryActuator();
        builder.Services.AddInfoActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient client = host.GetTestClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType.MediaType.Should().Be("application/json");

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "security_error": "Cloud controller URL is not available"
            }
            """);
    }

    [Fact]
    public async Task TargetEndpointNotConfiguredDelegatesToEndpointMiddleware()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["vcap:application:application_id"] = "foobar",
            ["vcap:application:cf_api"] = "http://localhost:9999/foo"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddCloudFoundryActuator();
        builder.Services.AddInfoActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient client = host.GetTestClient();

        HttpResponseMessage response =
            await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/does-not-exist"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.Should().BeNull();

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseBody.Should().BeEmpty();
    }

    [Fact]
    public async Task MissingAccessTokenReturnsUnauthorized()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["vcap:application:application_id"] = "foobar",
            ["vcap:application:cf_api"] = "http://localhost:9999/foo"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddCloudFoundryActuator();
        builder.Services.AddInfoActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient client = host.GetTestClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType.MediaType.Should().Be("application/json");

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "security_error": "Authorization header is missing or invalid"
            }
            """);
    }

    [Fact]
    public async Task UseStatusCodeFromResponseFalseReturnsOkAndContent()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:UseStatusCodeFromResponse"] = "false"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddCloudFoundryActuator();
        builder.Services.AddInfoActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient client = host.GetTestClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType.MediaType.Should().Be("application/json");

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "security_error": "Application ID is not available"
            }
            """);
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

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddCloudFoundryActuator();
        builder.Services.AddInfoActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient client = host.GetTestClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType.MediaType.Should().Be("application/json");

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "security_error": "Authorization header is missing or invalid"
            }
            """);
    }

    [Fact]
    public async Task SkipsSecurityCheckIfCloudFoundryDisabled()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:CloudFoundry:Enabled"] = "false"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddCloudFoundryActuator();
        builder.Services.AddInfoActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient client = host.GetTestClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseBody.Should().BeEmpty();
    }

    [Fact]
    public async Task DoesNotSkipSecurityCheckIfCloudFoundryActuatorDisabled()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:cloudfoundry:enabled"] = "false"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddCloudFoundryActuator();
        builder.Services.AddInfoActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient client = host.GetTestClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/info"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task GetAccessTokenReturnsExpected()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddCloudFoundryActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var permissionsProvider = serviceProvider.GetRequiredService<PermissionsProvider>();
        var endpointOptionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CloudFoundryEndpointOptions>>();
        var managementOptionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ManagementOptions>>();

        var middleware = new CloudFoundrySecurityMiddleware(managementOptionsMonitor, endpointOptionsMonitor, [], permissionsProvider,
            NullLogger<CloudFoundrySecurityMiddleware>.Instance, null);

        HttpContext context1 = CreateRequest("GET", "/");
        string token = middleware.GetAccessToken(context1.Request);
        token.Should().BeEmpty();

        HttpContext context2 = CreateRequest("GET", "/");
        context2.Request.Headers.Append("Authorization", new StringValues("Bearer foobar"));
        string token2 = middleware.GetAccessToken(context2.Request);
        token2.Should().Be("foobar");
    }

    [Fact]
    public async Task GetPermissionsReturnsExpected()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddCloudFoundryActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var permissionsProvider = serviceProvider.GetRequiredService<PermissionsProvider>();
        var endpointOptionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CloudFoundryEndpointOptions>>();
        var managementOptionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ManagementOptions>>();

        var middleware = new CloudFoundrySecurityMiddleware(managementOptionsMonitor, endpointOptionsMonitor, [], permissionsProvider,
            NullLogger<CloudFoundrySecurityMiddleware>.Instance, null);

        HttpContext context = CreateRequest("GET", "/");
        SecurityResult result = await middleware.GetPermissionsAsync(context);

        result.Permissions.Should().Be(EndpointPermissions.None);
        result.Code.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ThrowsWhenAddMethodNotCalled()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        await using WebApplication app = builder.Build();

        // ReSharper disable once AccessToDisposedClosure
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

    [Theory]
    [ClassData(typeof(CloudFoundrySecurityMiddlewareTestScenarios))]
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

    public void Dispose()
    {
        _scope.Dispose();
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

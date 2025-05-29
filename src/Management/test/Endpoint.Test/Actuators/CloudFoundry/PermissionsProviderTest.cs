// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;

namespace Steeltoe.Management.Endpoint.Test.Actuators.CloudFoundry;

public sealed class PermissionsProviderTest : BaseTest
{
    [Fact]
    public void IsCloudFoundryRequest_ReturnsExpected()
    {
        Assert.True(PermissionsProvider.IsCloudFoundryRequest("/cloudfoundryapplication"));
        Assert.True(PermissionsProvider.IsCloudFoundryRequest("/cloudfoundryapplication/badpath"));
    }

    [Fact]
    public async Task EmptyTokenIsUnauthorized()
    {
        PermissionsProvider permissionsProvider = GetPermissionsProvider();
        SecurityResult unauthorized = await permissionsProvider.GetPermissionsAsync(string.Empty, TestContext.Current.CancellationToken);
        unauthorized.Code.Should().Be(HttpStatusCode.Unauthorized);
        unauthorized.Message.Should().Be(PermissionsProvider.AuthorizationHeaderInvalid);
    }

    [Fact]
    public async Task GetPermissionsTest()
    {
        PermissionsProvider permissionsProvider = GetPermissionsProvider();
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        var permissions = new Dictionary<string, object>
        {
            ["read_sensitive_data"] = true
        };

        response.Content = JsonContent.Create(permissions);
        EndpointPermissions result = await permissionsProvider.ParsePermissionsAsync(response, TestContext.Current.CancellationToken);
        result.Should().Be(EndpointPermissions.Full);
    }

    private static PermissionsProvider GetPermissionsProvider()
    {
        IOptionsMonitor<CloudFoundryEndpointOptions> optionsMonitor = GetOptionsMonitorFromSettings<CloudFoundryEndpointOptions>();

        var services = new ServiceCollection();
        services.AddCloudFoundryActuator();
        using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        return new PermissionsProvider(optionsMonitor, httpClientFactory, NullLogger<PermissionsProvider>.Instance);
    }

    [InlineData("unavailable", HttpStatusCode.ServiceUnavailable, PermissionsProvider.CloudfoundryNotReachableMessage)]
    [InlineData("not-found", HttpStatusCode.Unauthorized, PermissionsProvider.InvalidTokenMessage)]
    [InlineData("unauthorized", HttpStatusCode.Unauthorized, PermissionsProvider.InvalidTokenMessage)]
    [InlineData("forbidden", HttpStatusCode.Forbidden, PermissionsProvider.AccessDeniedMessage)]
    [InlineData("timeout", HttpStatusCode.ServiceUnavailable, PermissionsProvider.CloudfoundryNotReachableMessage)]
    [InlineData("exception", HttpStatusCode.InternalServerError, "Exception of type 'System.Net.Http.HttpRequestException' was thrown.")]
    [InlineData("no_sensitive_data", HttpStatusCode.OK, "")]
    [InlineData("success", HttpStatusCode.OK, "")]
    [Theory]
    public async Task CloudControllerScenarioTesting(string cloudControllerResponse, HttpStatusCode steeltoeStatusCode, string? expectedMessage)
    {
        var appSettings = new Dictionary<string, string>
        {
            ["vcap:application:cf_api"] = "https://example.api.com",
            ["vcap:application:application_id"] = cloudControllerResponse
        };

        IOptionsMonitor<CloudFoundryEndpointOptions> optionsMonitor = GetOptionsMonitorFromSettings<CloudFoundryEndpointOptions>(appSettings!);

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddCloudFoundryActuator();

        services.AddHttpClient(PermissionsProvider.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(_ => CloudControllerPermissionsMock.GetHttpMessageHandler());

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        var permissionsProvider = new PermissionsProvider(optionsMonitor, httpClientFactory, NullLogger<PermissionsProvider>.Instance);

        SecurityResult result = await permissionsProvider.GetPermissionsAsync("testToken", TestContext.Current.CancellationToken);
        result.Code.Should().Be(steeltoeStatusCode);
        result.Message.Should().Be(expectedMessage);

        switch (cloudControllerResponse)
        {
            case "success":
                result.Permissions.Should().Be(EndpointPermissions.Full);
                break;
            case "no_sensitive_data":
                result.Permissions.Should().Be(EndpointPermissions.Restricted);
                break;
            default:
                result.Permissions.Should().Be(EndpointPermissions.None);
                break;
        }
    }
}

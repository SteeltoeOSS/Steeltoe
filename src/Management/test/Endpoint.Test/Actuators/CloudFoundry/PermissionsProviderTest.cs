// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Http.HttpClientPooling;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;

namespace Steeltoe.Management.Endpoint.Test.Actuators.CloudFoundry;

public sealed class PermissionsProviderTest
{
    [Fact]
    public void IsCloudFoundryRequestReturnsExpected()
    {
        PermissionsProvider.IsCloudFoundryRequest("/cloudfoundryapplication").Should().BeTrue();
        PermissionsProvider.IsCloudFoundryRequest("/cloudfoundryapplication/badpath").Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTokenIsUnauthorized()
    {
        PermissionsProvider permissionsProvider = GetPermissionsProvider();
        SecurityResult unauthorized = await permissionsProvider.GetPermissionsAsync(string.Empty, TestContext.Current.CancellationToken);
        unauthorized.Code.Should().Be(HttpStatusCode.Unauthorized);
        unauthorized.Message.Should().Be(PermissionsProvider.Messages.AuthorizationHeaderInvalid);
    }

    [Theory]
    [InlineData(false, true, EndpointPermissions.Restricted)]
    [InlineData(true, true, EndpointPermissions.Full)]
    public async Task ParsePermissionsResponseAsyncReturnsExpected(bool readSensitive, bool readBasic, EndpointPermissions expectedPermissions)
    {
        PermissionsProvider permissionsProvider = GetPermissionsProvider();

        var cloudControllerResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new Dictionary<string, bool>
            {
                ["read_sensitive_data"] = readSensitive,
                ["read_basic_data"] = readBasic
            })
        };

        EndpointPermissions result = await permissionsProvider.ParsePermissionsResponseAsync(cloudControllerResponse, TestContext.Current.CancellationToken);
        result.Should().Be(expectedPermissions);
    }

    [Theory]
    [InlineData("unavailable", HttpStatusCode.ServiceUnavailable, PermissionsProvider.Messages.CloudFoundryNotReachable)]
    [InlineData("not-found", HttpStatusCode.Unauthorized, PermissionsProvider.Messages.InvalidToken)]
    [InlineData("unauthorized", HttpStatusCode.Unauthorized, PermissionsProvider.Messages.InvalidToken)]
    [InlineData("forbidden", HttpStatusCode.Forbidden, PermissionsProvider.Messages.AccessDenied)]
    [InlineData("timeout", HttpStatusCode.ServiceUnavailable, PermissionsProvider.Messages.CloudFoundryTimeout)]
    [InlineData("exception", HttpStatusCode.ServiceUnavailable,
        "Exception of type 'System.Net.Http.HttpRequestException' with error 'NameResolutionError' was thrown")]
    [InlineData("no_sensitive_data", HttpStatusCode.OK, "")]
    [InlineData("success", HttpStatusCode.OK, "")]
    public async Task Returns_expected_response_on_permission_check(string scenario, HttpStatusCode? steeltoeStatusCode, string errorMessage)
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["vcap:application:cf_api"] = "https://example.api.com",
            ["vcap:application:application_id"] = scenario
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddCloudFoundryActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        serviceProvider.GetRequiredService<HttpClientHandlerFactory>().Using(CloudControllerPermissionsMock.GetHttpMessageHandler());
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CloudFoundryEndpointOptions>>();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        var permissionsProvider = new PermissionsProvider(optionsMonitor, httpClientFactory, NullLogger<PermissionsProvider>.Instance);

        SecurityResult result = await permissionsProvider.GetPermissionsAsync("testToken", TestContext.Current.CancellationToken);
        result.Code.Should().Be(steeltoeStatusCode);
        result.Message.Should().Be(errorMessage);

        switch (scenario)
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

    private static PermissionsProvider GetPermissionsProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddCloudFoundryActuator();
        using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CloudFoundryEndpointOptions>>();

        return new PermissionsProvider(optionsMonitor, httpClientFactory, NullLogger<PermissionsProvider>.Instance);
    }
}

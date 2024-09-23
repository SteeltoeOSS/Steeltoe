// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.HttpExchanges;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test.Actuators.HttpExchanges;

public sealed class EndpointMiddlewareTest : BaseTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["management:endpoints:enabled"] = "true",
        ["management:endpoints:httpExchanges:enabled"] = "true",
        ["management:endpoints:actuator:exposure:include:0"] = "httpexchanges"
    };

    [Fact]
    public async Task HttpExchangesActuator_DoesNotCaptureAuthInUri()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.AddHttpExchangesActuator();
        await using WebApplication host = builder.Build();

        host.UseRouting();
        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("http://username:password@localhost/actuator/httpexchanges");
        _ = await httpClient.GetAsync(requestUri);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync();
        json.Should().NotContain("username").And.NotContain("password");
        json.Should().Contain("http://localhost:80/actuator/httpexchanges");
    }

    [Fact]
    public async Task HttpExchangesActuator_ReturnsExpectedData()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.AddHttpExchangesActuator();
        await using WebApplication host = builder.Build();

        host.UseRouting();
        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/httpexchanges"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var httpExchanges = await response.Content.ReadFromJsonAsync<HttpExchangesResult>();
        httpExchanges!.Exchanges.Should().BeEmpty();

        response = await httpClient.GetAsync(new Uri("http://localhost/actuator/httpexchanges"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string json = await response.Content.ReadAsStringAsync();
        json.Should().NotContain("username").And.NotContain("password");
        json.Should().Contain("http://localhost:80/actuator/httpexchanges");
        json.Should().Contain($"\"timestamp\":\"{DateTime.Now.Date:yyyy-MM-dd}");
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var endpointOptions = GetOptionsFromSettings<HttpExchangesEndpointOptions>();
        ManagementOptions managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>().CurrentValue;

        endpointOptions.RequiresExactMatch().Should().BeTrue();
        endpointOptions.GetPathMatchPattern(managementOptions, managementOptions.Path).Should().Be("/actuator/httpexchanges");

        endpointOptions.GetPathMatchPattern(managementOptions, ConfigureManagementOptions.DefaultCloudFoundryPath).Should()
            .Be("/cloudfoundryapplication/httpexchanges");

        endpointOptions.AllowedVerbs.Should().Contain(verb => verb == "Get");
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.Refresh;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Refresh;

public sealed class HttpVerbInEndpointRoutingTest
{
    [Fact]
    public async Task Allows_only_POST_requests()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Actuator:Exposure:Include:0"] = "refresh"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddControllersWithViews();
        builder.Services.AddRefreshActuator();

        await using WebApplication app = builder.Build();
        app.UseRouting();
        app.MapDefaultControllerRoute();
        await app.StartAsync();

        using HttpClient httpClient = app.GetTestClient();
        var requestUri = new Uri("/actuator/refresh", UriKind.Relative);

        HttpResponseMessage getResponse = await httpClient.GetAsync(requestUri);
        getResponse.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);

        HttpResponseMessage postResponse = await httpClient.PostAsync(requestUri, null);
        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Can_be_configured_to_allow_no_verbs()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Actuator:Exposure:Include:0"] = "refresh",
            ["Management:Endpoints:Refresh:AllowedVerbs:0"] = string.Empty
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddControllersWithViews();
        builder.Services.AddRefreshActuator();

        await using WebApplication app = builder.Build();
        app.UseRouting();
        app.MapDefaultControllerRoute();
        await app.StartAsync();

        using HttpClient httpClient = app.GetTestClient();
        var requestUri = new Uri("/actuator/refresh", UriKind.Relative);

        HttpResponseMessage getResponse = await httpClient.GetAsync(requestUri);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        HttpResponseMessage postResponse = await httpClient.PostAsync(requestUri, null);
        postResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Can_be_configured_to_allow_only_GET_requests()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Actuator:Exposure:Include:0"] = "refresh",
            ["Management:Endpoints:Refresh:AllowedVerbs:0"] = "GET"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddControllersWithViews();
        builder.Services.AddRefreshActuator();

        await using WebApplication app = builder.Build();
        app.UseRouting();
        app.MapDefaultControllerRoute();
        await app.StartAsync();

        using HttpClient httpClient = app.GetTestClient();
        var requestUri = new Uri("/actuator/refresh", UriKind.Relative);

        HttpResponseMessage getResponse = await httpClient.GetAsync(requestUri);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage postResponse = await httpClient.PostAsync(requestUri, null);
        postResponse.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task Can_be_configured_to_allow_both_GET_and_POST_requests()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Actuator:Exposure:Include:0"] = "refresh",
            ["Management:Endpoints:Refresh:AllowedVerbs:0"] = "GET",
            ["Management:Endpoints:Refresh:AllowedVerbs:1"] = "POST"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddControllersWithViews();
        builder.Services.AddRefreshActuator();

        await using WebApplication app = builder.Build();
        app.UseRouting();
        app.MapDefaultControllerRoute();
        await app.StartAsync();

        using HttpClient httpClient = app.GetTestClient();
        var requestUri = new Uri("/actuator/refresh", UriKind.Relative);

        HttpResponseMessage getResponse = await httpClient.GetAsync(requestUri);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage postResponse = await httpClient.PostAsync(requestUri, null);
        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

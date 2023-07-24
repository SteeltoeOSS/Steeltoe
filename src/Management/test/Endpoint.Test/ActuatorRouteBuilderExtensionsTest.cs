// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Management.Endpoint.Options;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test;

public sealed class ActuatorRouteBuilderExtensionsTest
{
    [Fact]
    public async Task MapTestAuthSuccess()
    {
        IHostBuilder hostBuilder = GetHostBuilder(policy => policy.RequireClaim("scope", "actuators.read"));
        await ActAndAssertAsync(hostBuilder, true);
    }

    [Fact]
    public async Task MapTestAuthFail()
    {
        IHostBuilder hostBuilder = GetHostBuilder(policy => policy.RequireClaim("scope", "invalidscope"));
        await ActAndAssertAsync(hostBuilder, false);
    }

    private static IHostBuilder GetHostBuilder(Action<AuthorizationPolicyBuilder> policyAction)
    {
        var appSettings = new Dictionary<string, string>
        {
            { "management:endpoints:actuator:exposure:include:0", "*" }
        };

        return new HostBuilder().AddDynamicLogging().ConfigureServices((_, s) =>
        {
            s.AddAllActuators();
            s.AddRouting();
            s.AddActionDescriptorCollectionProvider();

            s.AddAuthentication(TestAuthHandler.AuthenticationScheme).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.AuthenticationScheme, _ =>
                {
                });

            s.AddAuthorization(options => options.AddPolicy("TestAuth", policyAction)); // setup Auth based on test Case
            s.AddServerSideBlazor();
        }).ConfigureWebHost(builder =>
        {
            builder.Configure(app => app.UseRouting().UseAuthentication().UseAuthorization().UseEndpoints(endpoints =>
            {
                endpoints.MapAllActuators().RequireAuthorization("TestAuth");
                endpoints.MapBlazorHub(); // https://github.com/SteeltoeOSS/Steeltoe/issues/729
            })).UseTestServer();
        }).ConfigureAppConfiguration(configure => configure.AddInMemoryCollection(appSettings));
    }

    private async Task ActAndAssertAsync(IHostBuilder hostBuilder, bool expectedSuccess)
    {
        using IHost host = await hostBuilder.StartAsync();
        using TestServer server = host.GetTestServer();

        IEnumerable<HttpMiddlewareOptions> optionsCollection = host.Services.GetServices<HttpMiddlewareOptions>();

        foreach (HttpMiddlewareOptions options in optionsCollection)
        {
            ManagementEndpointOptions managementOptions = host.Services.GetRequiredService<IOptionsMonitor<ManagementEndpointOptions>>().CurrentValue;
            string path = options.GetPathMatchPattern(managementOptions.Path, managementOptions);
            path = path.Replace("metrics/{**_}", "metrics", StringComparison.Ordinal);
            Assert.NotNull(path);
            HttpResponseMessage response;

            if (options.AllowedVerbs.Contains("Get"))
            {
                response = await server.CreateClient().GetAsync(new Uri(path, UriKind.RelativeOrAbsolute));
            }
            else
            {
                response = await server.CreateClient().PostAsync(new Uri(path, UriKind.RelativeOrAbsolute), null);
            }

            Assert.True(expectedSuccess == response.IsSuccessStatusCode,
                $"Expected {(expectedSuccess ? "success" : "failure")}, but got {response.StatusCode} for {path} and type {options}");
        }
    }
}

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
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test;

public sealed class ActuatorRouteBuilderExtensionsTest
{
    public static IEnumerable<object[]> ActuatorOptions
    {
        get
        {
            IHostBuilder hostBuilder = GetHostBuilder(policy => policy.RequireClaim("scope", "actuators.read"));

            using IHost host = hostBuilder.Build();

            return host.Services.GetServices<EndpointOptions>().Select(options => new object[]
            {
                options
            });
        }
    }

    [Theory]
    [MemberData(nameof(ActuatorOptions))]
    public async Task MapTestAuthSuccess(EndpointOptions options)
    {
        IHostBuilder hostBuilder = GetHostBuilder(policy => policy.RequireClaim("scope", "actuators.read"));
        await ActAndAssertAsync(hostBuilder, options, true);
    }

    [Theory]
    [MemberData(nameof(ActuatorOptions))]
    public async Task MapTestAuthFail(EndpointOptions options)
    {
        IHostBuilder hostBuilder = GetHostBuilder(policy => policy.RequireClaim("scope", "invalidscope"));
        await ActAndAssertAsync(hostBuilder, options, false);
    }

    private static IHostBuilder GetHostBuilder(Action<AuthorizationPolicyBuilder> policyAction)
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "management:endpoints:actuator:exposure:include:0", "*" }
        };

        return TestHostBuilderFactory.Create().ConfigureLogging(builder => builder.AddDynamicConsole()).ConfigureServices(services =>
        {
            services.AddAllActuators();

            services.AddAuthentication(TestAuthHandler.AuthenticationScheme).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.AuthenticationScheme, _ =>
                {
                });

            services.AddAuthorizationBuilder().AddPolicy("TestAuth", policyAction);
            services.AddServerSideBlazor();
        }).ConfigureWebHost(builder =>
        {
            builder.Configure(app => app.UseRouting().UseAuthentication().UseAuthorization().UseEndpoints(endpoints =>
            {
                endpoints.MapAllActuators().RequireAuthorization("TestAuth");
                endpoints.MapBlazorHub(); // https://github.com/SteeltoeOSS/Steeltoe/issues/729
            })).UseTestServer();
        }).ConfigureAppConfiguration(configure => configure.AddInMemoryCollection(appSettings));
    }

    private async Task ActAndAssertAsync(IHostBuilder builder, EndpointOptions options, bool expectedSuccess)
    {
        using IHost host = await builder.StartAsync();
        using TestServer server = host.GetTestServer();

        ManagementOptions managementOptions = host.Services.GetRequiredService<IOptionsMonitor<ManagementOptions>>().CurrentValue;
        string path = options.GetPathMatchPattern(managementOptions, managementOptions.Path);
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

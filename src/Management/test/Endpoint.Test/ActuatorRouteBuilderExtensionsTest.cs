// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test;

public class ActuatorRouteBuilderExtensionsTest
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
        return new HostBuilder().AddDynamicLogging().ConfigureServices((context, s) =>
        {
            s.AddTraceActuator(MediaTypeVersion.V1);
            s.AddThreadDumpActuator(MediaTypeVersion.V1);
            s.AddCloudFoundryActuator();
            s.AddAllActuators();
            s.AddRouting();

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
                endpoints.MapTheActuators().RequireAuthorization("TestAuth");
                endpoints.MapBlazorHub(); // https://github.com/SteeltoeOSS/Steeltoe/issues/729
            })).UseTestServer();
        });
    }

    private static ManagementEndpointOptions GetManagementContext(IServiceProvider services)
    {
        var mgmtOptions = services.GetService<IOptionsMonitor<ManagementEndpointOptions>>();
        return mgmtOptions.Get(ActuatorContext.Name);
    }

    private async Task ActAndAssertAsync(IHostBuilder hostBuilder, bool expectedSuccess)
    {
        using IHost host = await hostBuilder.StartAsync();
        using TestServer server = host.GetTestServer();

        IEnumerable<IEndpointOptions> optionsCollection = host.Services.GetServices<IEndpointOptions>();

        foreach (IEndpointOptions options in optionsCollection)
        {
            string path = options.GetContextPath(GetManagementContext(host.Services));

            Assert.NotNull(path);

            HttpResponseMessage response = await server.CreateClient().GetAsync(new Uri(path, UriKind.RelativeOrAbsolute));

            Assert.True(expectedSuccess == response.IsSuccessStatusCode,
                $"Expected {(expectedSuccess ? "success" : "failure")}, but got {response.StatusCode} for {path} and type {options}");
        }
    }
}

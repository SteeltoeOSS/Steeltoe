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
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test;

public sealed class ActuatorRouteBuilderExtensionsTest
{
    public static TheoryData<Type> EndpointOptionsTypes
    {
        get
        {
            List<Type> endpointOptionsType = typeof(ConfigureEndpointOptions<>).Assembly.GetTypes()
                .Where(type => type.IsAssignableTo(typeof(EndpointOptions)) && type != typeof(CloudFoundryEndpointOptions)).ToList();

            TheoryData<Type> theoryData = [];
            endpointOptionsType.ForEach(theoryData.Add);
            return theoryData;
        }
    }

    [Theory]
    [MemberData(nameof(EndpointOptionsTypes))]
    public async Task MapTestAuthSuccess(Type endpointOptionsType)
    {
        IHostBuilder hostBuilder = GetHostBuilder(policy => policy.RequireClaim("scope", "actuators.read"));
        await ActAndAssertAsync(hostBuilder, endpointOptionsType, true);
    }

    [Theory]
    [MemberData(nameof(EndpointOptionsTypes))]
    public async Task MapTestAuthFail(Type endpointOptionsType)
    {
        IHostBuilder hostBuilder = GetHostBuilder(policy => policy.RequireClaim("scope", "invalidscope"));
        await ActAndAssertAsync(hostBuilder, endpointOptionsType, false);
    }

    private static IHostBuilder GetHostBuilder(Action<AuthorizationPolicyBuilder> policyAction)
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "management:endpoints:actuator:exposure:include:0", "*" }
        };

        IHostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();
        hostBuilder.ConfigureLogging(builder => builder.AddDynamicConsole());
        hostBuilder.ConfigureAppConfiguration(configure => configure.AddInMemoryCollection(appSettings));

        hostBuilder.ConfigureServices(services =>
        {
            services.AddAllActuators();

            services.AddAuthentication(TestAuthHandler.AuthenticationScheme).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.AuthenticationScheme, _ =>
                {
                });

            services.AddAuthorizationBuilder().AddPolicy("TestAuth", policyAction);
            services.AddServerSideBlazor();

            // Workaround for service provider validation failure:
            //   Unable to resolve service for type 'Microsoft.AspNetCore.Components.PersistentComponentState' while attempting
            //   to activate 'Microsoft.AspNetCore.Components.Forms.DefaultAntiforgeryStateProvider'.
            // This happens because we're adding Blazor, but without using WebAssemblyHostBuilder.
            services.AddRazorComponents();
        });

        hostBuilder.ConfigureWebHost(builder =>
        {
            builder.Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapAllActuators().RequireAuthorization("TestAuth");
                    endpoints.MapBlazorHub(); // https://github.com/SteeltoeOSS/Steeltoe/issues/729
                });
            });
        });

        return hostBuilder;
    }

    private async Task ActAndAssertAsync(IHostBuilder builder, Type endpointOptionsType, bool expectedSuccess)
    {
        using IHost host = await builder.StartAsync();

        Type optionsMonitorType = typeof(IOptionsMonitor<>).MakeGenericType(endpointOptionsType);
        object optionsMonitor = host.Services.GetRequiredService(optionsMonitorType);
        var options = (EndpointOptions)((dynamic)optionsMonitor).CurrentValue;

        ManagementOptions managementOptions = host.Services.GetRequiredService<IOptionsMonitor<ManagementOptions>>().CurrentValue;
        string path = options.GetPathMatchPattern(managementOptions, managementOptions.Path);
        path = path.Replace("metrics/{**_}", "metrics", StringComparison.Ordinal);
        Assert.NotNull(path);
        HttpResponseMessage response;

        using HttpClient httpClient = host.GetTestClient();

        if (options.AllowedVerbs.Contains("Get"))
        {
            response = await httpClient.GetAsync(new Uri(path, UriKind.RelativeOrAbsolute));
        }
        else
        {
            response = await httpClient.PostAsync(new Uri(path, UriKind.RelativeOrAbsolute), null);
        }

        Assert.True(expectedSuccess == response.IsSuccessStatusCode,
            $"Expected {(expectedSuccess ? "success" : "failure")}, but got {response.StatusCode} for {path} and type {options}");
    }
}

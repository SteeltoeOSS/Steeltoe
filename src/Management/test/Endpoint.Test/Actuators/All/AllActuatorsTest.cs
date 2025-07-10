// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.All;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Actuators.DbMigrations;
using Steeltoe.Management.Endpoint.Actuators.Environment;
using Steeltoe.Management.Endpoint.Actuators.Health;
using Steeltoe.Management.Endpoint.Actuators.Health.Availability;
using Steeltoe.Management.Endpoint.Actuators.HeapDump;
using Steeltoe.Management.Endpoint.Actuators.HttpExchanges;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Endpoint.Actuators.Info;
using Steeltoe.Management.Endpoint.Actuators.Loggers;
using Steeltoe.Management.Endpoint.Actuators.Refresh;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings;
using Steeltoe.Management.Endpoint.Actuators.Services;
using Steeltoe.Management.Endpoint.Actuators.ThreadDump;
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.ManagementPort;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Test.Actuators.All;

public sealed class AllActuatorsTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Registers_dependent_services(bool platformIsCloudFoundry)
    {
        using IDisposable? scope = platformIsCloudFoundry ? new EnvironmentVariableScope("VCAP_APPLICATION", "{}") : null;

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<IWebHostEnvironment, FakeWebHostEnvironment>();
        services.AddAllActuators();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        List<Type> middlewareTypes =
        [
            typeof(HypermediaEndpointMiddleware),
            typeof(ThreadDumpEndpointMiddleware),
            typeof(HeapDumpEndpointMiddleware),
            typeof(DbMigrationsEndpointMiddleware),
            typeof(EnvironmentEndpointMiddleware),
            typeof(InfoEndpointMiddleware),
            typeof(HealthEndpointMiddleware),
            typeof(LoggersEndpointMiddleware),
            typeof(HttpExchangesEndpointMiddleware),
            typeof(RouteMappingsEndpointMiddleware),
            typeof(RefreshEndpointMiddleware),
            typeof(ServicesEndpointMiddleware)
        ];

        foreach (Type middlewareType in middlewareTypes)
        {
            // ReSharper disable once AccessToDisposedClosure
            Action action = () => serviceProvider.GetRequiredService(middlewareType);

            action.Should().NotThrow();
        }

        if (platformIsCloudFoundry)
        {
            Action action = () =>
            {
                // ReSharper disable AccessToDisposedClosure
                _ = serviceProvider.GetRequiredService<CloudFoundryEndpointMiddleware>();
                _ = serviceProvider.GetRequiredService<PermissionsProvider>();
                // ReSharper restore AccessToDisposedClosure
            };

            action.Should().NotThrow();
        }
        else
        {
            var middleware = serviceProvider.GetService<CloudFoundryEndpointMiddleware>();
            middleware.Should().BeNull();

            var provider = serviceProvider.GetService<PermissionsProvider>();
            provider.Should().BeNull();
        }
    }

    [Theory]
    [InlineData(HostBuilderType.Host, false)]
    [InlineData(HostBuilderType.Host, true)]
    [InlineData(HostBuilderType.WebHost, false)]
    [InlineData(HostBuilderType.WebHost, true)]
    [InlineData(HostBuilderType.WebApplication, false)]
    [InlineData(HostBuilderType.WebApplication, true)]
    public async Task Does_not_register_duplicate_services(HostBuilderType hostBuilderType, bool platformIsCloudFoundry)
    {
        using IDisposable? scope = platformIsCloudFoundry ? new EnvironmentVariableScope("VCAP_APPLICATION", "{}") : null;
        int actuatorCount = platformIsCloudFoundry ? 13 : 12;

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddAllActuators();
                services.AddAllActuators();
            });
        });

        host.Services.GetServices<ActuatorEndpointMapper>().Should().ContainSingle();
        host.Services.GetServices<IConfigureOptions<CorsOptions>>().OfType<ConfigureActuatorsCorsPolicyOptions>().Should().ContainSingle();
        host.Services.GetServices<IConfigureOptions<ManagementOptions>>().Should().ContainSingle();
        host.Services.GetServices<IOptionsChangeTokenSource<ManagementOptions>>().Should().ContainSingle();
        host.Services.GetServices<HasCloudFoundrySecurityMiddlewareMarker>().Should().ContainSingle();

        host.Services.GetServices<IEndpointOptionsMonitorProvider>().Should().HaveCount(actuatorCount);
        host.Services.GetServices<IEndpointMiddleware>().Should().HaveCount(actuatorCount);

        IStartupFilter[] startupFilters = [.. host.Services.GetServices<IStartupFilter>()];
        startupFilters.OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
        startupFilters.OfType<ManagementPortStartupFilter>().Should().ContainSingle();
        startupFilters.OfType<AvailabilityStartupFilter>().Should().ContainSingle();

        host.Services.GetServices<IConfigureOptions<InfoEndpointOptions>>().Should().ContainSingle();
        host.Services.GetServices<IOptionsChangeTokenSource<InfoEndpointOptions>>().Should().ContainSingle();
        host.Services.GetServices<IEndpointOptionsMonitorProvider>().OfType<EndpointOptionsMonitorProvider<InfoEndpointOptions>>().Should().ContainSingle();
        host.Services.GetServices<IInfoEndpointHandler>().OfType<InfoEndpointHandler>().Should().ContainSingle();
        host.Services.GetServices<InfoEndpointMiddleware>().Should().ContainSingle();
        host.Services.GetServices<IEndpointMiddleware>().OfType<InfoEndpointMiddleware>().Should().ContainSingle();
    }
}

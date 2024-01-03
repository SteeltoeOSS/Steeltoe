// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Hosting;
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.DbMigrations;
using Steeltoe.Management.Endpoint.Environment;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.ManagementPort;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Refresh;
using Steeltoe.Management.Endpoint.RouteMappings;
using Steeltoe.Management.Endpoint.Services;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;
using Steeltoe.Management.Endpoint.Web.Hypermedia;
using Steeltoe.Management.Info;

namespace Steeltoe.Management.Endpoint;

internal static class HostBuilderWrapperExtensions
{
    public static void AddAllActuators(this HostBuilderWrapper wrapper, Action<IEndpointConventionBuilder>? configureEndpoints,
        MediaTypeVersion mediaTypeVersion, Action<CorsPolicyBuilder>? buildCorsPolicy)
    {
        wrapper.ConfigureLogging(loggingBuilder => loggingBuilder.AddDynamicConsole());
        wrapper.ConfigureServices(services => services.AddAllActuators(mediaTypeVersion, buildCorsPolicy));
        RegisterActuatorEndpoints(wrapper, configureEndpoints);
    }

    public static void AddDbMigrationsActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services => services.AddDbMigrationsActuator());
        RegisterActuatorEndpoints(wrapper, null);
    }

    public static void AddEnvironmentActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services => services.AddEnvironmentActuator());
        RegisterActuatorEndpoints(wrapper, null);
    }

    public static void AddHealthActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services => services.AddHealthActuator());
        RegisterActuatorEndpoints(wrapper, null);
    }

    public static void AddHealthActuator(this HostBuilderWrapper wrapper, Type[] contributorTypes)
    {
        wrapper.ConfigureServices(services => services.AddHealthActuator(contributorTypes));
        RegisterActuatorEndpoints(wrapper, null);
    }

    public static void AddHealthActuator(this HostBuilderWrapper wrapper, IHealthAggregator aggregator, Type[] contributorTypes)
    {
        wrapper.ConfigureServices(services => services.AddHealthActuator(aggregator, contributorTypes));
        RegisterActuatorEndpoints(wrapper, null);
    }

    public static void AddHeapDumpActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services => services.AddHeapDumpActuator());
        RegisterActuatorEndpoints(wrapper, null);
    }

    public static void AddHypermediaActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services => services.AddHypermediaActuator());
        RegisterActuatorEndpoints(wrapper, null);
    }

    public static void AddInfoActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services => services.AddInfoActuator());
        RegisterActuatorEndpoints(wrapper, null);
    }

    public static void AddInfoActuator(this HostBuilderWrapper wrapper, IInfoContributor[] contributors)
    {
        wrapper.ConfigureServices(services => services.AddInfoActuator(contributors));
        RegisterActuatorEndpoints(wrapper, null);
    }

    public static void AddLoggersActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureLogging(loggingBuilder => loggingBuilder.AddDynamicConsole());
        wrapper.ConfigureServices(services => services.AddLoggersActuator());
        RegisterActuatorEndpoints(wrapper, null);
    }

    public static void AddMappingsActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services => services.AddMappingsActuator());
        RegisterActuatorEndpoints(wrapper, null);
    }

    public static void AddMetricsActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services => services.AddMetricsActuator());
        RegisterActuatorEndpoints(wrapper, null);
    }

    public static void AddRefreshActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services => services.AddRefreshActuator());
        RegisterActuatorEndpoints(wrapper, null);
    }

    public static void AddThreadDumpActuator(this HostBuilderWrapper wrapper, MediaTypeVersion mediaTypeVersion)
    {
        wrapper.ConfigureServices(services => services.AddThreadDumpActuator(mediaTypeVersion));
        RegisterActuatorEndpoints(wrapper, null);
    }

    public static void AddTraceActuator(this HostBuilderWrapper wrapper, MediaTypeVersion mediaTypeVersion)
    {
        wrapper.ConfigureServices(services => services.AddTraceActuator(mediaTypeVersion));
        RegisterActuatorEndpoints(wrapper, null);
    }

    public static void AddServicesActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services => services.AddServicesActuator());
        RegisterActuatorEndpoints(wrapper, null);
    }

    public static void AddCloudFoundryActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services => services.AddCloudFoundryActuator());
        RegisterActuatorEndpoints(wrapper, null);
    }

    private static void RegisterActuatorEndpoints(HostBuilderWrapper wrapper, Action<IEndpointConventionBuilder>? configureEndpoints)
    {
        wrapper.ConfigureServices(services =>
        {
            IEndpointConventionBuilder conventionBuilder = services.ActivateActuatorEndpoints();
            configureEndpoints?.Invoke(conventionBuilder);
        });

        wrapper.ConfigureWebHost(webHostBuilder => webHostBuilder.AddManagementPort());
    }
}

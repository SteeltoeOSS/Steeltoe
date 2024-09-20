// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Steeltoe.Common.Hosting;
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Actuators.DbMigrations;
using Steeltoe.Management.Endpoint.Actuators.Environment;
using Steeltoe.Management.Endpoint.Actuators.Health;
using Steeltoe.Management.Endpoint.Actuators.HeapDump;
using Steeltoe.Management.Endpoint.Actuators.HttpExchanges;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Endpoint.Actuators.Info;
using Steeltoe.Management.Endpoint.Actuators.Loggers;
using Steeltoe.Management.Endpoint.Actuators.Metrics;
using Steeltoe.Management.Endpoint.Actuators.Refresh;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings;
using Steeltoe.Management.Endpoint.Actuators.Services;
using Steeltoe.Management.Endpoint.Actuators.ThreadDump;

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

    public static void AddHttpExchangeActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services => services.AddHttpExchangesActuator());
        RegisterActuatorEndpoints(wrapper, null);
    }

    public static void AddServicesActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services => services.AddServicesActuator());
        RegisterActuatorEndpoints(wrapper, null);
    }

    public static void AddCloudFoundryActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services =>
        {
            services.AddCloudFoundryActuator();
            services.AddCloudFoundrySecurity();
        });

        RegisterActuatorEndpoints(wrapper, null);
    }

    private static void RegisterActuatorEndpoints(HostBuilderWrapper wrapper, Action<IEndpointConventionBuilder>? configureEndpoints)
    {
        wrapper.ConfigureServices(services =>
        {
            IEndpointConventionBuilder conventionBuilder = services.ActivateActuatorEndpoints();
            configureEndpoints?.Invoke(conventionBuilder);
        });
    }
}

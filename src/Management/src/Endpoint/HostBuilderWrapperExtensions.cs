// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Steeltoe.Common.Hosting;
using Steeltoe.Management.Endpoint.Actuators.All;
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
        MediaTypeVersion mediaTypeVersion)
    {
        wrapper.ConfigureServices(services =>
        {
            services.AddAllActuators(mediaTypeVersion, true);

            if (configureEndpoints != null)
            {
                services.ConfigureActuatorEndpoints(configureEndpoints);
            }
        });
    }

    public static void AddDbMigrationsActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services => services.AddDbMigrationsActuator());
    }

    public static void AddEnvironmentActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services => services.AddEnvironmentActuator());
    }

    public static void AddHealthActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services => services.AddHealthActuator());
    }

    public static void AddHeapDumpActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services => services.AddHeapDumpActuator());
    }

    public static void AddHypermediaActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services => services.AddHypermediaActuator());
    }

    public static void AddInfoActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services => services.AddInfoActuator());
    }

    public static void AddLoggersActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services => services.AddLoggersActuator());
    }

    public static void AddMappingsActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services => services.AddRouteMappingsActuator());
    }

    public static void AddMetricsActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services => services.AddMetricsActuator());
    }

    public static void AddRefreshActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services => services.AddRefreshActuator());
    }

    public static void AddThreadDumpActuator(this HostBuilderWrapper wrapper, MediaTypeVersion mediaTypeVersion)
    {
        wrapper.ConfigureServices(services => services.AddThreadDumpActuator(mediaTypeVersion, true));
    }

    public static void AddHttpExchangeActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services => services.AddHttpExchangesActuator());
    }

    public static void AddServicesActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services => services.AddServicesActuator());
    }

    public static void AddCloudFoundryActuator(this HostBuilderWrapper wrapper)
    {
        wrapper.ConfigureServices(services => services.AddCloudFoundryActuator());
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
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
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Refresh;
using Steeltoe.Management.Endpoint.Services;
using Steeltoe.Management.Endpoint.RouteMappings;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;
using Steeltoe.Management.Endpoint.Web.Hypermedia;
using Steeltoe.Management.Info;

namespace Steeltoe.Management.Endpoint;

public static class ManagementHostBuilderExtensions
{
    /// <summary>
    /// Adds the Database Migrations actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IHostBuilder AddDbMigrationsActuator(this IHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddDbMigrationsActuator();
            ActivateActuatorEndpoints(collection);
        });
    }

    /// <summary>
    /// Adds the Environment actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IHostBuilder AddEnvironmentActuator(this IHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddEnvironmentActuator();
            ActivateActuatorEndpoints(collection);
        });
    }

    /// <summary>
    /// Adds the Health actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IHostBuilder AddHealthActuator(this IHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddHealthActuator();
            ActivateActuatorEndpoints(collection);
        });
    }

    /// <summary>
    /// Adds the Health actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    /// <param name="contributorTypes">
    /// Types that contribute to the overall health of the app.
    /// </param>
    public static IHostBuilder AddHealthActuator(this IHostBuilder hostBuilder, params Type[] contributorTypes)
    {
        ArgumentGuard.NotNull(hostBuilder);
        ArgumentGuard.NotNull(contributorTypes);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddHealthActuator(contributorTypes);
            ActivateActuatorEndpoints(collection);
        });
    }

    /// <summary>
    /// Adds the Health actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    /// <param name="aggregator">
    /// Custom health aggregator.
    /// </param>
    /// <param name="contributorTypes">
    /// Types that contribute to the overall health of the app.
    /// </param>
    public static IHostBuilder AddHealthActuator(this IHostBuilder hostBuilder, IHealthAggregator aggregator, params Type[] contributorTypes)
    {
        ArgumentGuard.NotNull(hostBuilder);
        ArgumentGuard.NotNull(aggregator);
        ArgumentGuard.NotNull(contributorTypes);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddHealthActuator(aggregator, contributorTypes);
            ActivateActuatorEndpoints(collection);
        });
    }

    /// <summary>
    /// Adds the HeapDump actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IHostBuilder AddHeapDumpActuator(this IHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddHeapDumpActuator();
            ActivateActuatorEndpoints(collection);
        });
    }

    /// <summary>
    /// Adds the Hypermedia actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IHostBuilder AddHypermediaActuator(this IHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddHypermediaActuator();
            ActivateActuatorEndpoints(collection);
        });
    }

    /// <summary>
    /// Adds the Info actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IHostBuilder AddInfoActuator(this IHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddInfoActuator();
            ActivateActuatorEndpoints(collection);
        });
    }

    /// <summary>
    /// Adds the Info actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    /// <param name="contributors">
    /// Contributors to application information.
    /// </param>
    public static IHostBuilder AddInfoActuator(this IHostBuilder hostBuilder, params IInfoContributor[] contributors)
    {
        ArgumentGuard.NotNull(hostBuilder);
        ArgumentGuard.NotNull(contributors);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddInfoActuator(contributors);
            ActivateActuatorEndpoints(collection);
        });
    }

    /// <summary>
    /// Adds the Loggers actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IHostBuilder AddLoggersActuator(this IHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().AddDynamicLogging().ConfigureServices((_, collection) =>
        {
            collection.AddLoggersActuator();
            ActivateActuatorEndpoints(collection);
        });
    }

    /// <summary>
    /// Adds the Mappings actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IHostBuilder AddMappingsActuator(this IHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddMappingsActuator();
            ActivateActuatorEndpoints(collection);
        });
    }

    /// <summary>
    /// Adds the Metrics actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IHostBuilder AddMetricsActuator(this IHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddMetricsActuator();
            ActivateActuatorEndpoints(collection);
        });
    }

    /// <summary>
    /// Adds the Refresh actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IHostBuilder AddRefreshActuator(this IHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddRefreshActuator();
            ActivateActuatorEndpoints(collection);
        });
    }

    /// <summary>
    /// Adds the ThreadDump actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    /// <param name="mediaTypeVersion">
    /// Specify the media type version to use in the response.
    /// </param>
    public static IHostBuilder AddThreadDumpActuator(this IHostBuilder hostBuilder, MediaTypeVersion mediaTypeVersion)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddThreadDumpActuator(mediaTypeVersion);
            ActivateActuatorEndpoints(collection);
        });
    }

    /// <summary>
    /// Adds the ThreadDump actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IHostBuilder AddThreadDumpActuator(this IHostBuilder hostBuilder)
    {
        return hostBuilder.AddThreadDumpActuator(MediaTypeVersion.V2);
    }

    /// <summary>
    /// Adds the Trace actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    /// <param name="mediaTypeVersion">
    /// Specify the media type version to use in the response.
    /// </param>
    public static IHostBuilder AddTraceActuator(this IHostBuilder hostBuilder, MediaTypeVersion mediaTypeVersion)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddTraceActuator(mediaTypeVersion);
            ActivateActuatorEndpoints(collection);
        });
    }
    /// <summary>
    /// Adds the Services actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IHostBuilder AddServicesActuator(this IHostBuilder hostBuilder)
    {
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
        {
            collection.AddServicesActuator();
            ActivateActuatorEndpoints(collection);
        });
    }

    /// <summary>
    /// Adds the Trace actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IHostBuilder AddTraceActuator(this IHostBuilder hostBuilder)
    {
        return AddTraceActuator(hostBuilder, MediaTypeVersion.V2);
    }

    /// <summary>
    /// Adds the Cloud Foundry actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IHostBuilder AddCloudFoundryActuator(this IHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddCloudFoundryActuator();
            ActivateActuatorEndpoints(collection);
        });
    }

    /// <summary>
    /// Adds all standard actuators to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    /// <param name="configureEndpoints">
    /// Customize endpoint behavior. Useful for tailoring auth requirements.
    /// </param>
    /// <param name="mediaTypeVersion">
    /// Specify the media type version to use in the response.
    /// </param>
    /// <param name="buildCorsPolicy">
    /// Customize the CORS policy.
    /// </param>
    /// <remarks>
    /// Does not add platform specific features (like for Cloud Foundry or Kubernetes).
    /// </remarks>
    public static IHostBuilder AddAllActuators(this IHostBuilder hostBuilder, Action<IEndpointConventionBuilder>? configureEndpoints,
        MediaTypeVersion mediaTypeVersion, Action<CorsPolicyBuilder>? buildCorsPolicy)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddDynamicLogging().AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddAllActuators(mediaTypeVersion, buildCorsPolicy);
            IEndpointConventionBuilder endpointConventionBuilder = ActivateActuatorEndpoints(collection);
            configureEndpoints?.Invoke(endpointConventionBuilder);
        });
    }

    /// <summary>
    /// Adds all standard actuators to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    /// <remarks>
    /// Does not add platform specific features (like for Cloud Foundry or Kubernetes).
    /// </remarks>
    public static IHostBuilder AddAllActuators(this IHostBuilder hostBuilder)
    {
        return AddAllActuators(hostBuilder, null);
    }

    /// <summary>
    /// Adds all standard actuators to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    /// <param name="configureEndpoints">
    /// Customize endpoint behavior. Useful for tailoring auth requirements.
    /// </param>
    /// <param name="mediaTypeVersion">
    /// Specify the media type version to use in the response.
    /// </param>
    /// <remarks>
    /// Does not add platform specific features (like for Cloud Foundry or Kubernetes).
    /// </remarks>
    public static IHostBuilder AddAllActuators(this IHostBuilder hostBuilder, Action<IEndpointConventionBuilder>? configureEndpoints,
        MediaTypeVersion mediaTypeVersion)
    {
        return AddAllActuators(hostBuilder, configureEndpoints, mediaTypeVersion, null);
    }

    /// <summary>
    /// Adds all standard actuators to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    /// <param name="configureEndpoints">
    /// Customize endpoint behavior. Useful for tailoring auth requirements.
    /// </param>
    /// <remarks>
    /// Does not add platform specific features (like for Cloud Foundry or Kubernetes).
    /// </remarks>
    public static IHostBuilder AddAllActuators(this IHostBuilder hostBuilder, Action<IEndpointConventionBuilder>? configureEndpoints)
    {
        return AddAllActuators(hostBuilder, configureEndpoints, MediaTypeVersion.V2);
    }

    /// <summary>
    /// Registers an <see cref="IStartupFilter" /> that will map all configured actuators, initialize health.
    /// </summary>
    /// <param name="collection">
    /// <see cref="IServiceCollection" /> that has actuators to activate.
    /// </param>
    public static IEndpointConventionBuilder ActivateActuatorEndpoints(this IServiceCollection collection)
    {
        ArgumentGuard.NotNull(collection);

        // check for existing AllActuatorsStartupFilter
        IEnumerable<ServiceDescriptor> existingStartupFilters = collection.Where(descriptor =>
            descriptor.ImplementationType == typeof(AllActuatorsStartupFilter) ||
            descriptor.ImplementationFactory?.Method.ReturnType == typeof(AllActuatorsStartupFilter));

        var actuatorConventionBuilder = new ActuatorConventionBuilder();

        if (!existingStartupFilters.Any())
        {
            collection.AddTransient<IStartupFilter, AllActuatorsStartupFilter>(_ => new AllActuatorsStartupFilter(actuatorConventionBuilder));
        }

        return actuatorConventionBuilder;
    }

    private static IHostBuilder AddManagementPort(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureWebHost(webHostBuilder =>
        {
            (int? httpPort, int? httpsPort) = webHostBuilder.GetManagementPorts();

            if (httpPort.HasValue || httpsPort.HasValue)
            {
                webHostBuilder.UseCloudHosting(httpPort, httpsPort);
            }
        });
    }
}

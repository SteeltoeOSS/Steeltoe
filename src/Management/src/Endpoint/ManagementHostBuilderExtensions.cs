// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Hosting;
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.DbMigrations;
using Steeltoe.Management.Endpoint.Env;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Mappings;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Refresh;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;
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
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
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
    public static IHostBuilder AddEnvActuator(this IHostBuilder hostBuilder)
    {
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
        {
            collection.AddEnvActuator();
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
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
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
    /// <param name="contributors">
    /// Types that contribute to the overall health of the app.
    /// </param>
    public static IHostBuilder AddHealthActuator(this IHostBuilder hostBuilder, Type[] contributors)
    {
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
        {
            collection.AddHealthActuator(contributors);
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
    /// <param name="contributors">
    /// Types that contribute to the overall health of the app.
    /// </param>
    public static IHostBuilder AddHealthActuator(this IHostBuilder hostBuilder, IHealthAggregator aggregator, Type[] contributors)
    {
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
        {
            collection.AddHealthActuator(aggregator, contributors);
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
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
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
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
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
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
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
    public static IHostBuilder AddInfoActuator(this IHostBuilder hostBuilder, IInfoContributor[] contributors)
    {
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
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
        return hostBuilder.AddManagementPort().AddDynamicLogging().ConfigureServices((context, collection) =>
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
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
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
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
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
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
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
    public static IHostBuilder AddThreadDumpActuator(this IHostBuilder hostBuilder, MediaTypeVersion mediaTypeVersion = MediaTypeVersion.V2)
    {
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
        {
            collection.AddThreadDumpActuator(mediaTypeVersion);
            ActivateActuatorEndpoints(collection);
        });
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
    public static IHostBuilder AddTraceActuator(this IHostBuilder hostBuilder, MediaTypeVersion mediaTypeVersion = MediaTypeVersion.V2)
    {
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
        {
            collection.AddTraceActuator(mediaTypeVersion);
            ActivateActuatorEndpoints(collection);
        });
    }

    /// <summary>
    /// Adds the Cloud Foundry actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IHostBuilder AddCloudFoundryActuator(this IHostBuilder hostBuilder)
    {
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
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
    public static IHostBuilder AddAllActuators(this IHostBuilder hostBuilder, Action<IEndpointConventionBuilder> configureEndpoints = null,
        MediaTypeVersion mediaTypeVersion = MediaTypeVersion.V2, Action<CorsPolicyBuilder> buildCorsPolicy = null)
    {
        return hostBuilder.AddDynamicLogging().AddManagementPort().ConfigureServices((context, collection) =>
        {
            collection.AddAllActuators(mediaTypeVersion, buildCorsPolicy);
            IEndpointConventionBuilder endpointConventionBuilder = ActivateActuatorEndpoints(collection);
            configureEndpoints?.Invoke(endpointConventionBuilder);
        });
    }

    /// <summary>
    /// Registers an <see cref="IStartupFilter" /> that will map all configured actuators, initialize health.
    /// </summary>
    /// <param name="collection">
    /// <see cref="IServiceCollection" /> that has actuators to activate.
    /// </param>
    public static IEndpointConventionBuilder ActivateActuatorEndpoints(this IServiceCollection collection)
    {
        // check for existing AllActuatorsStartupFilter
        IEnumerable<ServiceDescriptor> existingStartupFilters = collection.Where(t =>
            t.ImplementationType == typeof(AllActuatorsStartupFilter) || t.ImplementationFactory?.Method?.ReturnType == typeof(AllActuatorsStartupFilter));

        var actuatorConventionBuilder = new ActuatorConventionBuilder();

        if (!existingStartupFilters.Any())
        {
            collection.AddTransient<IStartupFilter, AllActuatorsStartupFilter>(provider => new AllActuatorsStartupFilter(actuatorConventionBuilder));
        }

        return actuatorConventionBuilder;
    }

    private static IHostBuilder AddManagementPort(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureWebHost(webhostBuilder =>
        {
            webhostBuilder.GetManagementUrl(out int? httpPort, out int? httpsPort);

            if (httpPort.HasValue || httpsPort.HasValue)
            {
                webhostBuilder.UseCloudHosting(httpPort, httpsPort);
            }
        });
    }
}

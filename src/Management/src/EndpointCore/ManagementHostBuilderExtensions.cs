// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Extensions.Logging;
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
    /// <param name="hostBuilder">Your HostBuilder.</param>
    public static IHostBuilder AddDbMigrationsActuator(this IHostBuilder hostBuilder)
        => hostBuilder
            .ConfigureServices((context, collection) =>
            {
                collection.AddDbMigrationsActuator(context.Configuration);
                ActivateActuatorEndpoints(collection);
            });

    /// <summary>
    /// Adds the Environment actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder.</param>
    public static IHostBuilder AddEnvActuator(this IHostBuilder hostBuilder)
        => hostBuilder
            .ConfigureServices((context, collection) =>
            {
                collection.AddEnvActuator(context.Configuration);
                ActivateActuatorEndpoints(collection);
            });

    /// <summary>
    /// Adds the Health actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder.</param>
    public static IHostBuilder AddHealthActuator(this IHostBuilder hostBuilder)
        => hostBuilder
            .ConfigureServices((context, collection) =>
            {
                collection.AddHealthActuator(context.Configuration);
                ActivateActuatorEndpoints(collection);
            });

    /// <summary>
    /// Adds the Health actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder.</param>
    /// <param name="contributors">Types that contribute to the overall health of the app.</param>
    public static IHostBuilder AddHealthActuator(this IHostBuilder hostBuilder, Type[] contributors)
        => hostBuilder
            .ConfigureServices((context, collection) =>
            {
                collection.AddHealthActuator(context.Configuration, contributors);
                ActivateActuatorEndpoints(collection);
            });

    /// <summary>
    /// Adds the Health actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder.</param>
    /// <param name="aggregator">Custom health aggregator.</param>
    /// <param name="contributors">Types that contribute to the overall health of the app.</param>
    public static IHostBuilder AddHealthActuator(this IHostBuilder hostBuilder, IHealthAggregator aggregator, Type[] contributors)
        => hostBuilder
            .ConfigureServices((context, collection) =>
            {
                collection.AddHealthActuator(context.Configuration, aggregator, contributors);
                ActivateActuatorEndpoints(collection);
            });

    /// <summary>
    /// Adds the HeapDump actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder.</param>
    public static IHostBuilder AddHeapDumpActuator(this IHostBuilder hostBuilder)
        => hostBuilder
            .ConfigureServices((context, collection) =>
            {
                collection.AddHeapDumpActuator(context.Configuration);
                ActivateActuatorEndpoints(collection);
            });

    /// <summary>
    /// Adds the Hypermedia actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder.</param>
    public static IHostBuilder AddHypermediaActuator(this IHostBuilder hostBuilder)
        => hostBuilder
            .ConfigureServices((context, collection) =>
            {
                collection.AddHypermediaActuator(context.Configuration);
                ActivateActuatorEndpoints(collection);
            });

    /// <summary>
    /// Adds the Info actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder.</param>
    public static IHostBuilder AddInfoActuator(this IHostBuilder hostBuilder)
        => hostBuilder
            .ConfigureServices((context, collection) =>
            {
                collection.AddInfoActuator(context.Configuration);
                ActivateActuatorEndpoints(collection);
            });

    /// <summary>
    /// Adds the Info actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder.</param>
    /// <param name="contributors">Contributors to application information.</param>
    public static IHostBuilder AddInfoActuator(this IHostBuilder hostBuilder, IInfoContributor[] contributors)
        => hostBuilder
            .ConfigureServices((context, collection) =>
            {
                collection.AddInfoActuator(context.Configuration, contributors);
                ActivateActuatorEndpoints(collection);
            });

    /// <summary>
    /// Adds the Loggers actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder.</param>
    public static IHostBuilder AddLoggersActuator(this IHostBuilder hostBuilder)
        => hostBuilder
            .AddDynamicLogging()
            .ConfigureServices((context, collection) =>
            {
                collection.AddLoggersActuator(context.Configuration);
                ActivateActuatorEndpoints(collection);
            });

    /// <summary>
    /// Adds the Mappings actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder.</param>
    public static IHostBuilder AddMappingsActuator(this IHostBuilder hostBuilder)
        => hostBuilder
            .ConfigureServices((context, collection) =>
            {
                collection.AddMappingsActuator(context.Configuration);
                ActivateActuatorEndpoints(collection);
            });

    /// <summary>
    /// Adds the Metrics actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder.</param>
    public static IHostBuilder AddMetricsActuator(this IHostBuilder hostBuilder)
        => hostBuilder
            .ConfigureServices((context, collection) =>
            {
                collection.AddMetricsActuator(context.Configuration);
                ActivateActuatorEndpoints(collection);
            });

    /// <summary>
    /// Adds the Refresh actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder.</param>
    public static IHostBuilder AddRefreshActuator(this IHostBuilder hostBuilder)
        => hostBuilder
            .ConfigureServices((context, collection) =>
            {
                collection.AddRefreshActuator(context.Configuration);
                ActivateActuatorEndpoints(collection);
            });

    /// <summary>
    /// Adds the ThreadDump actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder.</param>
    /// <param name="mediaTypeVersion">Specify the media type version to use in the response.</param>
    public static IHostBuilder AddThreadDumpActuator(this IHostBuilder hostBuilder, MediaTypeVersion mediaTypeVersion = MediaTypeVersion.V2)
        => hostBuilder
            .ConfigureServices((context, collection) =>
            {
                collection.AddThreadDumpActuator(context.Configuration, mediaTypeVersion);
                ActivateActuatorEndpoints(collection);
            });

    /// <summary>
    /// Adds the Trace actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder.</param>
    /// <param name="mediaTypeVersion">Specify the media type version to use in the response.</param>
    public static IHostBuilder AddTraceActuator(this IHostBuilder hostBuilder, MediaTypeVersion mediaTypeVersion = MediaTypeVersion.V2)
        => hostBuilder
            .ConfigureServices((context, collection) =>
            {
                collection.AddTraceActuator(context.Configuration, mediaTypeVersion);
                ActivateActuatorEndpoints(collection);
            });

    /// <summary>
    /// Adds the Cloud Foundry actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder.</param>
    public static IHostBuilder AddCloudFoundryActuator(this IHostBuilder hostBuilder)
        => hostBuilder
            .ConfigureServices((context, collection) =>
            {
                collection.AddCloudFoundryActuator(context.Configuration);
                ActivateActuatorEndpoints(collection);
            });

    /// <summary>
    /// Adds all standard actuators to the application.
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder.</param>
    /// <param name="configureEndpoints">Customize endpoint behavior. Useful for tailoring auth requirements.</param>
    /// <param name="mediaTypeVersion">Specify the media type version to use in the response.</param>
    /// <param name="buildCorsPolicy">Customize the CORS policy. </param>
    /// <remarks>Does not add platform specific features (like for Cloud Foundry or Kubernetes).</remarks>
    public static IHostBuilder AddAllActuators(this IHostBuilder hostBuilder, Action<IEndpointConventionBuilder> configureEndpoints = null, MediaTypeVersion mediaTypeVersion = MediaTypeVersion.V2, Action<CorsPolicyBuilder> buildCorsPolicy = null)
        => hostBuilder
            .AddDynamicLogging()
            .ConfigureServices((context, collection) =>
            {
                collection.AddAllActuators(context.Configuration, mediaTypeVersion, buildCorsPolicy);
                ActivateActuatorEndpoints(collection, configureEndpoints);
            });

    /// <summary>
    /// Add wavefront metrics to the application.
    /// </summary>
    /// <param name="hostBuilder">Your Hostbuilder.</param>
    /// <returns>The updated HostBuilder.</returns>
    public static IHostBuilder AddWavefrontMetrics(this IHostBuilder hostBuilder)
        => hostBuilder
            .ConfigureServices((_, collection) =>
            {
                collection.AddWavefrontMetrics();
            });

    /// <summary>
    /// Registers an <see cref="IStartupFilter" /> that will map all configured actuators, initialize health.
    /// </summary>
    /// <param name="collection"><see cref="IServiceCollection" /> that has actuators to activate.</param>
    /// <param name="configureEndpoints">IEndpointConventionBuilder customizations (such as auth policy customization).</param>
    public static void ActivateActuatorEndpoints(this IServiceCollection collection, Action<IEndpointConventionBuilder> configureEndpoints = null)
    {
        // check for existing AllActuatorsStartupFilter
        var existingStartupFilters = collection.Where(t => t.ImplementationType == typeof(AllActuatorsStartupFilter) || t.ImplementationFactory?.Method?.ReturnType == typeof(AllActuatorsStartupFilter));

        // if we have an Action<IEndpointConventionBuilder> and there isn't one, add a new one
        if (configureEndpoints != null)
        {
            // remove any existing AllActuatorsStartupFilter registration
            foreach (var f in existingStartupFilters.ToList())
            {
                collection.Remove(f);
            }

            // add a registration that includes this endpoint configuration
            collection.AddTransient<IStartupFilter, AllActuatorsStartupFilter>(_ => new AllActuatorsStartupFilter(configureEndpoints));
        }
        else
        {
            // make sure there is (only) one AllActuatorsStartupFilter
            if (!existingStartupFilters.Any())
            {
                collection.AddTransient<IStartupFilter, AllActuatorsStartupFilter>();
            }
        }
    }
}

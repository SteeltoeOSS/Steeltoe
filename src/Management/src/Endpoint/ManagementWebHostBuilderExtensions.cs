// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Hosting;
using Steeltoe.Management.Info;

namespace Steeltoe.Management.Endpoint;

public static class ManagementWebHostBuilderExtensions
{
    /// <summary>
    /// Adds the Database Migrations actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddDbMigrationsActuator(this IWebHostBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddDbMigrationsActuator();

        return builder;
    }

    /// <summary>
    /// Adds the Environment actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddEnvironmentActuator(this IWebHostBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddEnvironmentActuator();

        return builder;
    }

    /// <summary>
    /// Adds the Health actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddHealthActuator(this IWebHostBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddHealthActuator();

        return builder;
    }

    /// <summary>
    /// Adds the Health actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <param name="contributorTypes">
    /// Types that contribute to the overall health of the app.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddHealthActuator(this IWebHostBuilder builder, params Type[] contributorTypes)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(contributorTypes);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddHealthActuator(contributorTypes);

        return builder;
    }

    /// <summary>
    /// Adds the Health actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <param name="aggregator">
    /// Custom health aggregator.
    /// </param>
    /// <param name="contributorTypes">
    /// Types that contribute to the overall health of the app.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddHealthActuator(this IWebHostBuilder builder, IHealthAggregator aggregator, params Type[] contributorTypes)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(aggregator);
        ArgumentGuard.NotNull(contributorTypes);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddHealthActuator(aggregator, contributorTypes);

        return builder;
    }

    /// <summary>
    /// Adds the HeapDump actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddHeapDumpActuator(this IWebHostBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddHeapDumpActuator();

        return builder;
    }

    /// <summary>
    /// Adds the Hypermedia actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddHypermediaActuator(this IWebHostBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddHypermediaActuator();

        return builder;
    }

    /// <summary>
    /// Adds the Info actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddInfoActuator(this IWebHostBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddInfoActuator();

        return builder;
    }

    /// <summary>
    /// Adds the Info actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <param name="contributors">
    /// Contributors to application information.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddInfoActuator(this IWebHostBuilder builder, params IInfoContributor[] contributors)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(contributors);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddInfoActuator(contributors);

        return builder;
    }

    /// <summary>
    /// Adds the Loggers actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddLoggersActuator(this IWebHostBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddLoggersActuator();

        return builder;
    }

    /// <summary>
    /// Adds the Mappings actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddMappingsActuator(this IWebHostBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddMappingsActuator();

        return builder;
    }

    /// <summary>
    /// Adds the Metrics actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddMetricsActuator(this IWebHostBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddMetricsActuator();

        return builder;
    }

    /// <summary>
    /// Adds the Refresh actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddRefreshActuator(this IWebHostBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddRefreshActuator();

        return builder;
    }

    /// <summary>
    /// Adds the ThreadDump actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddThreadDumpActuator(this IWebHostBuilder builder)
    {
        return AddThreadDumpActuator(builder, MediaTypeVersion.V2);
    }

    /// <summary>
    /// Adds the ThreadDump actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <param name="mediaTypeVersion">
    /// Specify the media type version to use in the response.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddThreadDumpActuator(this IWebHostBuilder builder, MediaTypeVersion mediaTypeVersion)
    {
        ArgumentGuard.NotNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddThreadDumpActuator(mediaTypeVersion);

        return builder;
    }

    /// <summary>
    /// Adds the Trace actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddTraceActuator(this IWebHostBuilder builder)
    {
        return AddTraceActuator(builder, MediaTypeVersion.V2);
    }

    /// <summary>
    /// Adds the Trace actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <param name="mediaTypeVersion">
    /// Specify the media type version to use in the response.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddTraceActuator(this IWebHostBuilder builder, MediaTypeVersion mediaTypeVersion)
    {
        ArgumentGuard.NotNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddTraceActuator(mediaTypeVersion);

        return builder;
    }

    /// <summary>
    /// Adds the Cloud Foundry actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddCloudFoundryActuator(this IWebHostBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddCloudFoundryActuator();

        return builder;
    }

    /// <summary>
    /// Adds all Steeltoe Actuators to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddAllActuators(this IWebHostBuilder builder)
    {
        return AddAllActuators(builder, null);
    }

    /// <summary>
    /// Adds all Steeltoe Actuators to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <param name="configureEndpoints">
    /// <see cref="IEndpointConventionBuilder" />.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddAllActuators(this IWebHostBuilder builder, Action<IEndpointConventionBuilder>? configureEndpoints)
    {
        return AddAllActuators(builder, configureEndpoints, MediaTypeVersion.V2, null);
    }

    /// <summary>
    /// Adds all Steeltoe Actuators to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <param name="configureEndpoints">
    /// <see cref="IEndpointConventionBuilder" />.
    /// </param>
    /// <param name="mediaTypeVersion">
    /// Specify the media type version to use in the response.
    /// </param>
    /// <param name="buildCorsPolicy">
    /// Customize the CORS policy.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddAllActuators(this IWebHostBuilder builder, Action<IEndpointConventionBuilder>? configureEndpoints,
        MediaTypeVersion mediaTypeVersion, Action<CorsPolicyBuilder>? buildCorsPolicy)
    {
        ArgumentGuard.NotNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddAllActuators(configureEndpoints, mediaTypeVersion, buildCorsPolicy);

        return builder;
    }
}

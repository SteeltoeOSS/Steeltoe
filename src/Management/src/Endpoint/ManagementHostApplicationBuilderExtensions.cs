// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Hosting;

namespace Steeltoe.Management.Endpoint;

public static class ManagementHostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the Database Migrations actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddDbMigrationsActuator(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddDbMigrationsActuator();

        return builder;
    }

    /// <summary>
    /// Adds the Environment actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddEnvironmentActuator(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddEnvironmentActuator();

        return builder;
    }

    /// <summary>
    /// Adds the Health actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddHealthActuator(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddHealthActuator();

        return builder;
    }

    /// <summary>
    /// Adds the HeapDump actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddHeapDumpActuator(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddHeapDumpActuator();

        return builder;
    }

    /// <summary>
    /// Adds the Hypermedia actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddHypermediaActuator(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddHypermediaActuator();

        return builder;
    }

    /// <summary>
    /// Adds the Info actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddInfoActuator(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddInfoActuator();

        return builder;
    }

    /// <summary>
    /// Adds the Loggers actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddLoggersActuator(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddLoggersActuator();

        return builder;
    }

    /// <summary>
    /// Adds the Mappings actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddMappingsActuator(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddMappingsActuator();

        return builder;
    }

    /// <summary>
    /// Adds the Metrics actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddMetricsActuator(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddMetricsActuator();

        return builder;
    }

    /// <summary>
    /// Adds the Refresh actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddRefreshActuator(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddRefreshActuator();

        return builder;
    }

    /// <summary>
    /// Adds the ThreadDump actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddThreadDumpActuator(this IHostApplicationBuilder builder)
    {
        return AddThreadDumpActuator(builder, MediaTypeVersion.V2);
    }

    /// <summary>
    /// Adds the ThreadDump actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <param name="mediaTypeVersion">
    /// Specify the media type version to use in the response.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddThreadDumpActuator(this IHostApplicationBuilder builder, MediaTypeVersion mediaTypeVersion)
    {
        ArgumentNullException.ThrowIfNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddThreadDumpActuator(mediaTypeVersion);

        return builder;
    }

    /// <summary>
    /// Adds the Trace actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddTraceActuator(this IHostApplicationBuilder builder)
    {
        return AddTraceActuator(builder, MediaTypeVersion.V2);
    }

    /// <summary>
    /// Adds the Trace actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <param name="mediaTypeVersion">
    /// Specify the media type version to use in the response.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddTraceActuator(this IHostApplicationBuilder builder, MediaTypeVersion mediaTypeVersion)
    {
        ArgumentNullException.ThrowIfNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddTraceActuator(mediaTypeVersion);

        return builder;
    }

    /// <summary>
    /// Adds an actuator endpoint that lists all injectable services that are registered in the IoC container.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddServicesActuator(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddServicesActuator();

        return builder;
    }

    /// <summary>
    /// Adds the Cloud Foundry actuator to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddCloudFoundryActuator(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddCloudFoundryActuator();

        return builder;
    }

    /// <summary>
    /// Adds all Steeltoe Actuators to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddAllActuators(this IHostApplicationBuilder builder)
    {
        return AddAllActuators(builder, null);
    }

    /// <summary>
    /// Adds all Steeltoe Actuators to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <param name="configureEndpoints">
    /// <see cref="IEndpointConventionBuilder" />.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddAllActuators(this IHostApplicationBuilder builder, Action<IEndpointConventionBuilder>? configureEndpoints)
    {
        return AddAllActuators(builder, configureEndpoints, MediaTypeVersion.V2, null);
    }

    /// <summary>
    /// Adds all Steeltoe Actuators to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
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
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddAllActuators(this IHostApplicationBuilder builder, Action<IEndpointConventionBuilder>? configureEndpoints,
        MediaTypeVersion mediaTypeVersion, Action<CorsPolicyBuilder>? buildCorsPolicy)
    {
        ArgumentNullException.ThrowIfNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddAllActuators(configureEndpoints, mediaTypeVersion, buildCorsPolicy);

        return builder;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
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

public static class ManagementWebApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the Database Migrations actuator to the application.
    /// </summary>
    /// <param name="applicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    public static WebApplicationBuilder AddDbMigrationsActuator(this WebApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services.AddDbMigrationsActuator(applicationBuilder.Configuration);
        applicationBuilder.Services.ActivateActuatorEndpoints();
        return applicationBuilder;
    }

    /// <summary>
    /// Adds the Environment actuator to the application.
    /// </summary>
    /// <param name="applicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    public static WebApplicationBuilder AddEnvActuator(this WebApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services.AddEnvActuator(applicationBuilder.Configuration);
        applicationBuilder.Services.ActivateActuatorEndpoints();
        return applicationBuilder;
    }

    /// <summary>
    /// Adds the Health actuator to the application.
    /// </summary>
    /// <param name="applicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    public static WebApplicationBuilder AddHealthActuator(this WebApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services.AddHealthActuator(applicationBuilder.Configuration);
        applicationBuilder.Services.ActivateActuatorEndpoints();
        return applicationBuilder;
    }

    /// <summary>
    /// Adds the Health actuator to the application.
    /// </summary>
    /// <param name="applicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    /// <param name="contributors">
    /// Types that contribute to the overall health of the app.
    /// </param>
    public static WebApplicationBuilder AddHealthActuator(this WebApplicationBuilder applicationBuilder, Type[] contributors)
    {
        applicationBuilder.Services.AddHealthActuator(applicationBuilder.Configuration, contributors);
        applicationBuilder.Services.ActivateActuatorEndpoints();
        return applicationBuilder;
    }

    /// <summary>
    /// Adds the Health actuator to the application.
    /// </summary>
    /// <param name="applicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    /// <param name="aggregator">
    /// Custom health aggregator.
    /// </param>
    /// <param name="contributors">
    /// Types that contribute to the overall health of the app.
    /// </param>
    public static WebApplicationBuilder AddHealthActuator(this WebApplicationBuilder applicationBuilder, IHealthAggregator aggregator, Type[] contributors)
    {
        applicationBuilder.Services.AddHealthActuator(applicationBuilder.Configuration, aggregator, contributors);
        applicationBuilder.Services.ActivateActuatorEndpoints();
        return applicationBuilder;
    }

    /// <summary>
    /// Adds the HeapDump actuator to the application.
    /// </summary>
    /// <param name="applicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    public static WebApplicationBuilder AddHeapDumpActuator(this WebApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services.AddHeapDumpActuator(applicationBuilder.Configuration);
        applicationBuilder.Services.ActivateActuatorEndpoints();
        return applicationBuilder;
    }

    /// <summary>
    /// Adds the Hypermedia actuator to the application.
    /// </summary>
    /// <param name="applicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    public static WebApplicationBuilder AddHypermediaActuator(this WebApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services.AddHypermediaActuator(applicationBuilder.Configuration);
        applicationBuilder.Services.ActivateActuatorEndpoints();
        return applicationBuilder;
    }

    /// <summary>
    /// Adds the Info actuator to the application.
    /// </summary>
    /// <param name="applicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    public static WebApplicationBuilder AddInfoActuator(this WebApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services.AddInfoActuator(applicationBuilder.Configuration);
        applicationBuilder.Services.ActivateActuatorEndpoints();
        return applicationBuilder;
    }

    /// <summary>
    /// Adds the Info actuator to the application.
    /// </summary>
    /// <param name="applicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    /// <param name="contributors">
    /// Contributors to application information.
    /// </param>
    public static WebApplicationBuilder AddInfoActuator(this WebApplicationBuilder applicationBuilder, IInfoContributor[] contributors)
    {
        applicationBuilder.Services.AddInfoActuator(applicationBuilder.Configuration, contributors);
        applicationBuilder.Services.ActivateActuatorEndpoints();
        return applicationBuilder;
    }

    /// <summary>
    /// Adds the Loggers actuator to the application.
    /// </summary>
    /// <param name="applicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    public static WebApplicationBuilder AddLoggersActuator(this WebApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Logging.AddDynamicConsole();
        applicationBuilder.Services.AddLoggersActuator(applicationBuilder.Configuration);
        applicationBuilder.Services.ActivateActuatorEndpoints();
        return applicationBuilder;
    }

    /// <summary>
    /// Adds the Mappings actuator to the application.
    /// </summary>
    /// <param name="applicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    public static WebApplicationBuilder AddMappingsActuator(this WebApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services.AddMappingsActuator(applicationBuilder.Configuration);
        applicationBuilder.Services.ActivateActuatorEndpoints();
        return applicationBuilder;
    }

    /// <summary>
    /// Adds the Metrics actuator to the application.
    /// </summary>
    /// <param name="applicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    public static WebApplicationBuilder AddMetricsActuator(this WebApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services.AddMetricsActuator(applicationBuilder.Configuration);
        applicationBuilder.Services.ActivateActuatorEndpoints();
        return applicationBuilder;
    }

    /// <summary>
    /// Adds the Refresh actuator to the application.
    /// </summary>
    /// <param name="applicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    public static WebApplicationBuilder AddRefreshActuator(this WebApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services.AddRefreshActuator(applicationBuilder.Configuration);
        applicationBuilder.Services.ActivateActuatorEndpoints();
        return applicationBuilder;
    }

    /// <summary>
    /// Adds the ThreadDump actuator to the application.
    /// </summary>
    /// <param name="applicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    /// <param name="mediaTypeVersion">
    /// Specify the media type version to use in the response.
    /// </param>
    public static WebApplicationBuilder AddThreadDumpActuator(this WebApplicationBuilder applicationBuilder,
        MediaTypeVersion mediaTypeVersion = MediaTypeVersion.V2)
    {
        applicationBuilder.Services.AddThreadDumpActuator(applicationBuilder.Configuration, mediaTypeVersion);
        applicationBuilder.Services.ActivateActuatorEndpoints();
        return applicationBuilder;
    }

    /// <summary>
    /// Adds the Trace actuator to the application.
    /// </summary>
    /// <param name="applicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    /// <param name="mediaTypeVersion">
    /// Specify the media type version to use in the response.
    /// </param>
    public static WebApplicationBuilder AddTraceActuator(this WebApplicationBuilder applicationBuilder, MediaTypeVersion mediaTypeVersion = MediaTypeVersion.V2)
    {
        applicationBuilder.Services.AddTraceActuator(applicationBuilder.Configuration, mediaTypeVersion);
        applicationBuilder.Services.ActivateActuatorEndpoints();
        return applicationBuilder;
    }

    /// <summary>
    /// Adds the Cloud Foundry actuator to the application.
    /// </summary>
    /// <param name="applicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    public static WebApplicationBuilder AddCloudFoundryActuator(this WebApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services.AddCloudFoundryActuator(applicationBuilder.Configuration);
        applicationBuilder.Services.ActivateActuatorEndpoints();
        return applicationBuilder;
    }

    /// <summary>
    /// Adds all Steeltoe Actuators to the application.
    /// </summary>
    /// <param name="applicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    /// <param name="configureEndpoints">
    /// <see cref="IEndpointConventionBuilder" />.
    /// </param>
    /// <param name="mediaTypeVersion">
    /// Specify the media type version to use in the response.
    /// </param>
    public static WebApplicationBuilder AddAllActuators(this WebApplicationBuilder applicationBuilder,
        Action<IEndpointConventionBuilder> configureEndpoints = null, MediaTypeVersion mediaTypeVersion = MediaTypeVersion.V2)
    {
        applicationBuilder.Logging.AddDynamicConsole();
        applicationBuilder.Services.AddAllActuators(applicationBuilder.Configuration, mediaTypeVersion);
        applicationBuilder.Services.ActivateActuatorEndpoints(configureEndpoints);
        return applicationBuilder;
    }

    /// <summary>
    /// Add Wavefront Metrics Exporter.
    /// </summary>
    /// <param name="applicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    public static WebApplicationBuilder AddWavefrontMetrics(this WebApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services.AddWavefrontMetrics();
        return applicationBuilder;
    }
}

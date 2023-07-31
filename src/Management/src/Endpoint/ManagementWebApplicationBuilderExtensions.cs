// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
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
using Steeltoe.Management.Endpoint.RouteMappings;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;
using Steeltoe.Management.Endpoint.Web.Hypermedia;
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
        ArgumentGuard.NotNull(applicationBuilder);

        applicationBuilder.Services.AddDbMigrationsActuator();
        applicationBuilder.Services.ActivateActuatorEndpoints();
        return applicationBuilder;
    }

    /// <summary>
    /// Adds the Environment actuator to the application.
    /// </summary>
    /// <param name="applicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    public static WebApplicationBuilder AddEnvironmentActuator(this WebApplicationBuilder applicationBuilder)
    {
        ArgumentGuard.NotNull(applicationBuilder);

        applicationBuilder.Services.AddEnvironmentActuator();
        applicationBuilder.AddCommonServices();
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
        ArgumentGuard.NotNull(applicationBuilder);

        applicationBuilder.Services.AddHealthActuator();
        applicationBuilder.AddCommonServices();
        return applicationBuilder;
    }

    /// <summary>
    /// Adds the Health actuator to the application.
    /// </summary>
    /// <param name="applicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    /// <param name="contributorTypes">
    /// Types that contribute to the overall health of the app.
    /// </param>
    public static WebApplicationBuilder AddHealthActuator(this WebApplicationBuilder applicationBuilder, params Type[] contributorTypes)
    {
        ArgumentGuard.NotNull(applicationBuilder);
        ArgumentGuard.NotNull(contributorTypes);

        applicationBuilder.Services.AddHealthActuator(contributorTypes);
        applicationBuilder.AddCommonServices();
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
    /// <param name="contributorTypes">
    /// Types that contribute to the overall health of the app.
    /// </param>
    public static WebApplicationBuilder AddHealthActuator(this WebApplicationBuilder applicationBuilder, IHealthAggregator aggregator,
        params Type[] contributorTypes)
    {
        ArgumentGuard.NotNull(applicationBuilder);
        ArgumentGuard.NotNull(aggregator);
        ArgumentGuard.NotNull(contributorTypes);

        applicationBuilder.Services.AddHealthActuator(aggregator, contributorTypes);
        applicationBuilder.AddCommonServices();
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
        ArgumentGuard.NotNull(applicationBuilder);

        applicationBuilder.Services.AddHeapDumpActuator();
        applicationBuilder.AddCommonServices();
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
        ArgumentGuard.NotNull(applicationBuilder);

        applicationBuilder.Services.AddHypermediaActuator();
        applicationBuilder.AddCommonServices();
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
        ArgumentGuard.NotNull(applicationBuilder);

        applicationBuilder.Services.AddInfoActuator();
        applicationBuilder.AddCommonServices();
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
    public static WebApplicationBuilder AddInfoActuator(this WebApplicationBuilder applicationBuilder, params IInfoContributor[] contributors)
    {
        ArgumentGuard.NotNull(applicationBuilder);
        ArgumentGuard.NotNull(contributors);

        applicationBuilder.Services.AddInfoActuator(contributors);
        applicationBuilder.AddCommonServices();
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
        ArgumentGuard.NotNull(applicationBuilder);

        applicationBuilder.Logging.AddDynamicConsole();
        applicationBuilder.Services.AddLoggersActuator();
        applicationBuilder.AddCommonServices();
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
        ArgumentGuard.NotNull(applicationBuilder);

        applicationBuilder.Services.AddMappingsActuator();
        applicationBuilder.AddCommonServices();
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
        ArgumentGuard.NotNull(applicationBuilder);

        applicationBuilder.Services.AddMetricsActuator();
        applicationBuilder.AddCommonServices();
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
        ArgumentGuard.NotNull(applicationBuilder);

        applicationBuilder.Services.AddRefreshActuator();
        applicationBuilder.AddCommonServices();
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
    public static WebApplicationBuilder AddThreadDumpActuator(this WebApplicationBuilder applicationBuilder, MediaTypeVersion mediaTypeVersion)
    {
        ArgumentGuard.NotNull(applicationBuilder);

        applicationBuilder.Services.AddThreadDumpActuator(mediaTypeVersion);
        applicationBuilder.AddCommonServices();
        return applicationBuilder;
    }

    /// <summary>
    /// Adds the ThreadDump actuator to the application.
    /// </summary>
    /// <param name="applicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    public static WebApplicationBuilder AddThreadDumpActuator(this WebApplicationBuilder applicationBuilder)
    {
        return AddThreadDumpActuator(applicationBuilder, MediaTypeVersion.V2);
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
    public static WebApplicationBuilder AddTraceActuator(this WebApplicationBuilder applicationBuilder, MediaTypeVersion mediaTypeVersion)
    {
        ArgumentGuard.NotNull(applicationBuilder);

        applicationBuilder.Services.AddTraceActuator(mediaTypeVersion);
        applicationBuilder.AddCommonServices();
        return applicationBuilder;
    }

    /// <summary>
    /// Adds the Trace actuator to the application.
    /// </summary>
    /// <param name="applicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    public static WebApplicationBuilder AddTraceActuator(this WebApplicationBuilder applicationBuilder)
    {
        return AddTraceActuator(applicationBuilder, MediaTypeVersion.V2);
    }

    /// <summary>
    /// Adds the Cloud Foundry actuator to the application.
    /// </summary>
    /// <param name="applicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    public static WebApplicationBuilder AddCloudFoundryActuator(this WebApplicationBuilder applicationBuilder)
    {
        ArgumentGuard.NotNull(applicationBuilder);

        applicationBuilder.Services.AddCloudFoundryActuator();
        applicationBuilder.AddCommonServices();
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
    public static WebApplicationBuilder AddAllActuators(this WebApplicationBuilder applicationBuilder, Action<IEndpointConventionBuilder> configureEndpoints,
        MediaTypeVersion mediaTypeVersion)
    {
        ArgumentGuard.NotNull(applicationBuilder);

        applicationBuilder.Logging.AddDynamicConsole();
        applicationBuilder.Services.AddAllActuators(mediaTypeVersion);
        applicationBuilder.AddCommonServices(configureEndpoints);
        return applicationBuilder;
    }

    /// <summary>
    /// Adds all Steeltoe Actuators to the application.
    /// </summary>
    /// <param name="applicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    public static WebApplicationBuilder AddAllActuators(this WebApplicationBuilder applicationBuilder)
    {
        return AddAllActuators(applicationBuilder, null);
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
    public static WebApplicationBuilder AddAllActuators(this WebApplicationBuilder applicationBuilder, Action<IEndpointConventionBuilder> configureEndpoints)
    {
        return AddAllActuators(applicationBuilder, configureEndpoints, MediaTypeVersion.V2);
    }

    private static void AddCommonServices(this WebApplicationBuilder applicationBuilder, Action<IEndpointConventionBuilder> configureEndpoints = null)
    {
        (int? httpPort, int? httpsPort) = applicationBuilder.WebHost.GetManagementPorts();

        if (httpPort.HasValue || httpsPort.HasValue)
        {
            applicationBuilder.UseCloudHosting(httpPort, httpsPort);
        }

        IEndpointConventionBuilder endpointConventionBuilder = applicationBuilder.Services.ActivateActuatorEndpoints();
        configureEndpoints?.Invoke(endpointConventionBuilder);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
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

public static class ManagementWebHostBuilderExtensions
{
    private const string ManagementPortKey = "management:endpoints:port";
    private const string ManagementSslKey = "management:endpoints:sslenabled";

    /// <summary>
    /// Adds the Database Migrations actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IWebHostBuilder AddDbMigrationsActuator(this IWebHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddDbMigrationsActuator();
            collection.ActivateActuatorEndpoints();
        });
    }

    /// <summary>
    /// Adds the Environment actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IWebHostBuilder AddEnvironmentActuator(this IWebHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddEnvironmentActuator();
            collection.ActivateActuatorEndpoints();
        });
    }

    /// <summary>
    /// Adds the Health actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IWebHostBuilder AddHealthActuator(this IWebHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddHealthActuator();
            collection.ActivateActuatorEndpoints();
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
    public static IWebHostBuilder AddHealthActuator(this IWebHostBuilder hostBuilder, params Type[] contributorTypes)
    {
        ArgumentGuard.NotNull(hostBuilder);
        ArgumentGuard.NotNull(contributorTypes);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddHealthActuator(contributorTypes);
            collection.ActivateActuatorEndpoints();
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
    public static IWebHostBuilder AddHealthActuator(this IWebHostBuilder hostBuilder, IHealthAggregator aggregator, params Type[] contributorTypes)
    {
        ArgumentGuard.NotNull(hostBuilder);
        ArgumentGuard.NotNull(aggregator);
        ArgumentGuard.NotNull(contributorTypes);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddHealthActuator(aggregator, contributorTypes);
            collection.ActivateActuatorEndpoints();
        });
    }

    /// <summary>
    /// Adds the HeapDump actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IWebHostBuilder AddHeapDumpActuator(this IWebHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddHeapDumpActuator();
            collection.ActivateActuatorEndpoints();
        });
    }

    /// <summary>
    /// Adds the Hypermedia actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IWebHostBuilder AddHypermediaActuator(this IWebHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddHypermediaActuator();
            collection.ActivateActuatorEndpoints();
        });
    }

    /// <summary>
    /// Adds the Info actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IWebHostBuilder AddInfoActuator(this IWebHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddInfoActuator();
            collection.ActivateActuatorEndpoints();
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
    public static IWebHostBuilder AddInfoActuator(this IWebHostBuilder hostBuilder, params IInfoContributor[] contributors)
    {
        ArgumentGuard.NotNull(hostBuilder);
        ArgumentGuard.NotNull(contributors);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddInfoActuator(contributors);
            collection.ActivateActuatorEndpoints();
        });
    }

    /// <summary>
    /// Adds the Loggers actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IWebHostBuilder AddLoggersActuator(this IWebHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().ConfigureLogging(builder => builder.AddDynamicConsole()).ConfigureServices((_, collection) =>
        {
            collection.AddLoggersActuator();
            collection.ActivateActuatorEndpoints();
        });
    }

    /// <summary>
    /// Adds the Mappings actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IWebHostBuilder AddMappingsActuator(this IWebHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddMappingsActuator();
            collection.ActivateActuatorEndpoints();
        });
    }

    /// <summary>
    /// Adds the Metrics actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IWebHostBuilder AddMetricsActuator(this IWebHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddMetricsActuator();
            collection.ActivateActuatorEndpoints();
        });
    }

    /// <summary>
    /// Adds the Refresh actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IWebHostBuilder AddRefreshActuator(this IWebHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddRefreshActuator();
            collection.ActivateActuatorEndpoints();
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
    public static IWebHostBuilder AddThreadDumpActuator(this IWebHostBuilder hostBuilder, MediaTypeVersion mediaTypeVersion)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddThreadDumpActuator(mediaTypeVersion);
            collection.ActivateActuatorEndpoints();
        });
    }

    /// <summary>
    /// Adds the ThreadDump actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IWebHostBuilder AddThreadDumpActuator(this IWebHostBuilder hostBuilder)
    {
        return AddThreadDumpActuator(hostBuilder, MediaTypeVersion.V2);
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
    public static IWebHostBuilder AddTraceActuator(this IWebHostBuilder hostBuilder, MediaTypeVersion mediaTypeVersion)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddTraceActuator(mediaTypeVersion);
            collection.ActivateActuatorEndpoints();
        });
    }

    /// <summary>
    /// Adds the Trace actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IWebHostBuilder AddTraceActuator(this IWebHostBuilder hostBuilder)
    {
        return hostBuilder.AddTraceActuator(MediaTypeVersion.V2);
    }

    /// <summary>
    /// Adds the Cloud Foundry actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IWebHostBuilder AddCloudFoundryActuator(this IWebHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().ConfigureServices((_, collection) =>
        {
            collection.AddCloudFoundryActuator();
            collection.ActivateActuatorEndpoints();
        });
    }

    /// <summary>
    /// Adds all Steeltoe Actuators to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    /// <param name="configureEndpoints">
    /// <see cref="IEndpointConventionBuilder" />.
    /// </param>
    /// <param name="mediaTypeVersion">
    /// Specify the media type version to use in the response.
    /// </param>
    public static IWebHostBuilder AddAllActuators(this IWebHostBuilder hostBuilder, Action<IEndpointConventionBuilder> configureEndpoints,
        MediaTypeVersion mediaTypeVersion)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.AddManagementPort().ConfigureLogging(builder => builder.AddDynamicConsole()).ConfigureServices((_, collection) =>
        {
            collection.AddAllActuators(mediaTypeVersion);
            IEndpointConventionBuilder conventionBuilder = collection.ActivateActuatorEndpoints();
            configureEndpoints?.Invoke(conventionBuilder);
        });
    }

    /// <summary>
    /// Adds all Steeltoe Actuators to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    /// <param name="configureEndpoints">
    /// <see cref="IEndpointConventionBuilder" />.
    /// </param>
    public static IWebHostBuilder AddAllActuators(this IWebHostBuilder hostBuilder, Action<IEndpointConventionBuilder> configureEndpoints)
    {
        return AddAllActuators(hostBuilder, configureEndpoints, MediaTypeVersion.V2);
    }

    /// <summary>
    /// Adds all Steeltoe Actuators to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IWebHostBuilder AddAllActuators(this IWebHostBuilder hostBuilder)
    {
        return AddAllActuators(hostBuilder, null);
    }

    internal static void GetManagementUrl(this IWebHostBuilder webHostBuilder, out int? httpPort, out int? httpsPort)
    {
        ArgumentGuard.NotNull(webHostBuilder);

        string portSetting = webHostBuilder.GetSetting(ManagementPortKey);
        string sslSetting = webHostBuilder.GetSetting(ManagementSslKey);

        httpPort = httpsPort = null;

        if (string.IsNullOrEmpty(portSetting))
        {
            IConfiguration configuration = GetConfigurationFallback(); // try reading directly from appsettings.json
            portSetting = configuration?[ManagementPortKey];
            sslSetting = configuration?[ManagementSslKey];
        }

        if (int.TryParse(portSetting, out int managementPort) && managementPort > 0)
        {
            if (bool.TryParse(sslSetting, out bool enableSsl) && enableSsl)
            {
                httpsPort = managementPort;
            }
            else
            {
                httpPort = managementPort;
            }
        }
    }

    private static IWebHostBuilder AddManagementPort(this IWebHostBuilder webHostBuilder)
    {
        webHostBuilder.GetManagementUrl(out int? httpPort, out int? httpsPort);

        if (httpPort.HasValue || httpsPort.HasValue)
        {
            webHostBuilder.UseCloudHosting(httpPort, httpsPort);
        }

        return webHostBuilder;
    }

    private static IConfiguration GetConfigurationFallback()
    {
        IConfiguration configuration = null;

        try
        {
            string environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddJsonFile($"appsettings.{environment}.json", true).Build();
        }
        catch (Exception)
        {
            // Not much we can do ...
        }

        return configuration;
    }
}

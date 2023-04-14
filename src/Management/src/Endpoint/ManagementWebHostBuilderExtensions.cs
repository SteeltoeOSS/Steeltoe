// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
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

public static class ManagementWebHostBuilderExtensions
{
    public const string ManagementPortKey = "management:endpoints:port";
    public const string ManagementSSLKey = "management:endpoints:sslenabled";

    /// <summary>
    /// Adds the Database Migrations actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IWebHostBuilder AddDbMigrationsActuator(this IWebHostBuilder hostBuilder)
    {
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
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
    public static IWebHostBuilder AddEnvActuator(this IWebHostBuilder hostBuilder)
    {
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
        {
            collection.AddEnvActuator();
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
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
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
    /// <param name="contributors">
    /// Types that contribute to the overall health of the app.
    /// </param>
    public static IWebHostBuilder AddHealthActuator(this IWebHostBuilder hostBuilder, Type[] contributors)
    {
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
        {
            collection.AddHealthActuator(contributors);
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
    /// <param name="contributors">
    /// Types that contribute to the overall health of the app.
    /// </param>
    public static IWebHostBuilder AddHealthActuator(this IWebHostBuilder hostBuilder, IHealthAggregator aggregator, Type[] contributors)
    {
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
        {
            collection.AddHealthActuator(aggregator, contributors);
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
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
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
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
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
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
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
    public static IWebHostBuilder AddInfoActuator(this IWebHostBuilder hostBuilder, IInfoContributor[] contributors)
    {
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
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
        return hostBuilder.AddManagementPort().ConfigureLogging(builder => builder.AddDynamicConsole()).ConfigureServices((context, collection) =>
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
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
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
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
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
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
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
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
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
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
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
    public static IWebHostBuilder AddTraceActuator(this IWebHostBuilder hostBuilder) => hostBuilder.AddTraceActuator(MediaTypeVersion.V2);
    /// <summary>
    /// Adds the Cloud Foundry actuator to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IWebHostBuilder AddCloudFoundryActuator(this IWebHostBuilder hostBuilder)
    {
        return hostBuilder.AddManagementPort().ConfigureServices((context, collection) =>
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
        return hostBuilder.AddManagementPort().ConfigureLogging(builder => builder.AddDynamicConsole()).ConfigureServices((context, collection) =>
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
        string portSetting = webHostBuilder.GetSetting(ManagementPortKey);
        string sslSetting = webHostBuilder.GetSetting(ManagementSSLKey);

        httpPort = httpsPort = null;

        if (string.IsNullOrEmpty(portSetting))
        {
            IConfiguration config = GetConfigurationFallback(); // try reading directly from appsettings.json
            portSetting = config?[ManagementPortKey];
            sslSetting = config?[ManagementSSLKey];
        }

        if (int.TryParse(portSetting, out int intManagementPort))
        {
            bool.TryParse(sslSetting, out bool sslEnabled);

            if (intManagementPort > 0)
            {
                if (sslEnabled)
                {
                    httpsPort = intManagementPort;
                }
                else
                {
                    httpPort = intManagementPort;
                }
            }
        }
    }

    private static IWebHostBuilder AddManagementPort(this IWebHostBuilder webhostBuilder)
    {
        webhostBuilder.GetManagementUrl(out int? httpPort, out int? httpsPort);

        if (httpPort.HasValue || httpsPort.HasValue)
        {
            webhostBuilder.UseCloudHosting(httpPort, httpsPort);
        }

        return webhostBuilder;
    }

    private static IConfiguration GetConfigurationFallback()
    {
        IConfiguration config = null;

        try
        {
            string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            config = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddJsonFile($"appsettings.{environment}.json", true).Build();
        }
        catch (Exception)
        {
            // Not much we can do ...
        }

        return config;
    }
}

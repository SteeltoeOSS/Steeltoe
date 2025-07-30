// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Hosting;
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
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint;

public static class ManagementWebHostBuilderExtensions
{
    public const string ManagementPortKey = "management:endpoints:port";
    public const string ManagementSSLKey = "management:endpoints:sslenabled";

    /// <summary>
    /// Adds the Database Migrations actuator to the application
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder</param>
    public static IWebHostBuilder AddDbMigrationsActuator(this IWebHostBuilder hostBuilder)
        => hostBuilder.AddManagementPort()
            .ConfigureServices((context, collection) =>
            {
                collection.AddDbMigrationsActuator(context.Configuration);
                collection.ActivateActuatorEndpoints();
            });

    /// <summary>
    /// Adds the Environment actuator to the application
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder</param>
    public static IWebHostBuilder AddEnvActuator(this IWebHostBuilder hostBuilder)
        => hostBuilder.AddManagementPort()
            .ConfigureServices((context, collection) =>
            {
                collection.AddEnvActuator(context.Configuration);
                collection.ActivateActuatorEndpoints();
            });

    /// <summary>
    /// Adds the Health actuator to the application
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder</param>
    public static IWebHostBuilder AddHealthActuator(this IWebHostBuilder hostBuilder)
        => hostBuilder.AddManagementPort()
            .ConfigureServices((context, collection) =>
            {
                collection.AddHealthActuator(context.Configuration);
                collection.ActivateActuatorEndpoints();
            });

    /// <summary>
    /// Adds the Health actuator to the application
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder</param>
    /// <param name="contributors">Types that contribute to the overall health of the app</param>
    public static IWebHostBuilder AddHealthActuator(this IWebHostBuilder hostBuilder, Type[] contributors)
        => hostBuilder.AddManagementPort()
            .ConfigureServices((context, collection) =>
            {
                collection.AddHealthActuator(context.Configuration, contributors);
                collection.ActivateActuatorEndpoints();
            });

    /// <summary>
    /// Adds the Health actuator to the application
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder</param>
    /// <param name="aggregator">Custom health aggregator</param>
    /// <param name="contributors">Types that contribute to the overall health of the app</param>
    public static IWebHostBuilder AddHealthActuator(this IWebHostBuilder hostBuilder, IHealthAggregator aggregator, Type[] contributors)
        => hostBuilder.AddManagementPort()
            .ConfigureServices((context, collection) =>
            {
                collection.AddHealthActuator(context.Configuration, aggregator, contributors);
                collection.ActivateActuatorEndpoints();
            });

    /// <summary>
    /// Adds the HeapDump actuator to the application
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder</param>
    public static IWebHostBuilder AddHeapDumpActuator(this IWebHostBuilder hostBuilder)
        => hostBuilder.AddManagementPort()
            .ConfigureServices((context, collection) =>
            {
                collection.AddHeapDumpActuator(context.Configuration);
                collection.ActivateActuatorEndpoints();
            });

    /// <summary>
    /// Adds the Hypermedia actuator to the application
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder</param>
    public static IWebHostBuilder AddHypermediaActuator(this IWebHostBuilder hostBuilder)
        => hostBuilder.AddManagementPort()
            .ConfigureServices((context, collection) =>
            {
                collection.AddHypermediaActuator(context.Configuration);
                collection.ActivateActuatorEndpoints();
            });

    /// <summary>
    /// Adds the Info actuator to the application
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder</param>
    public static IWebHostBuilder AddInfoActuator(this IWebHostBuilder hostBuilder)
        => hostBuilder.AddManagementPort()
            .ConfigureServices((context, collection) =>
            {
                collection.AddInfoActuator(context.Configuration);
                collection.ActivateActuatorEndpoints();
            });

    /// <summary>
    /// Adds the Info actuator to the application
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder</param>
    /// <param name="contributors">Contributors to application information</param>
    public static IWebHostBuilder AddInfoActuator(this IWebHostBuilder hostBuilder, IInfoContributor[] contributors)
        => hostBuilder.AddManagementPort()
            .ConfigureServices((context, collection) =>
            {
                collection.AddInfoActuator(context.Configuration, contributors);
                collection.ActivateActuatorEndpoints();
            });

    /// <summary>
    /// Adds the Loggers actuator to the application
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder</param>
    public static IWebHostBuilder AddLoggersActuator(this IWebHostBuilder hostBuilder)
        => hostBuilder.AddManagementPort()
            .ConfigureLogging(builder => builder.AddDynamicConsole())
            .ConfigureServices((context, collection) =>
            {
                collection.AddLoggersActuator(context.Configuration);
                collection.ActivateActuatorEndpoints();
            });

    /// <summary>
    /// Adds the Mappings actuator to the application
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder</param>
    public static IWebHostBuilder AddMappingsActuator(this IWebHostBuilder hostBuilder)
        => hostBuilder.AddManagementPort()
            .ConfigureServices((context, collection) =>
            {
                collection.AddMappingsActuator(context.Configuration);
                collection.ActivateActuatorEndpoints();
            });

    /// <summary>
    /// Adds the Metrics actuator to the application
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder</param>
    public static IWebHostBuilder AddMetricsActuator(this IWebHostBuilder hostBuilder)
        => hostBuilder.AddManagementPort()
            .ConfigureServices((context, collection) =>
            {
                collection.AddMetricsActuator(context.Configuration);
                collection.ActivateActuatorEndpoints();
            });

    /// <summary>
    /// Adds the Refresh actuator to the application
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder</param>
    public static IWebHostBuilder AddRefreshActuator(this IWebHostBuilder hostBuilder)
        => hostBuilder.AddManagementPort()
            .ConfigureServices((context, collection) =>
            {
                collection.AddRefreshActuator(context.Configuration);
                collection.ActivateActuatorEndpoints();
            });

    /// <summary>
    /// Adds the ThreadDump actuator to the application
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder</param>
    /// <param name="mediaTypeVersion">Specify the media type version to use in the response</param>
    public static IWebHostBuilder AddThreadDumpActuator(this IWebHostBuilder hostBuilder, MediaTypeVersion mediaTypeVersion = MediaTypeVersion.V2)
        => hostBuilder.AddManagementPort()
            .ConfigureServices((context, collection) =>
            {
                collection.AddThreadDumpActuator(context.Configuration, mediaTypeVersion);
                collection.ActivateActuatorEndpoints();
            });

    /// <summary>
    /// Adds the Trace actuator to the application
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder</param>
    /// <param name="mediaTypeVersion">Specify the media type version to use in the response</param>
    public static IWebHostBuilder AddTraceActuator(this IWebHostBuilder hostBuilder, MediaTypeVersion mediaTypeVersion = MediaTypeVersion.V2)
        => hostBuilder.AddManagementPort()
            .ConfigureServices((context, collection) =>
            {
                collection.AddTraceActuator(context.Configuration, mediaTypeVersion);
                collection.ActivateActuatorEndpoints();
            });

    /// <summary>
    /// Adds the Cloud Foundry actuator to the application
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder</param>
    public static IWebHostBuilder AddCloudFoundryActuator(this IWebHostBuilder hostBuilder)
        => hostBuilder.AddManagementPort()
            .ConfigureServices((context, collection) =>
            {
                collection.AddCloudFoundryActuator(context.Configuration);
                collection.ActivateActuatorEndpoints();
            });

    /// <summary>
    /// Adds all Steeltoe Actuators to the application
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder</param>
    /// <param name="configureEndpoints"><see cref="IEndpointConventionBuilder" /></param>
    /// <param name="mediaTypeVersion">Specify the media type version to use in the response</param>
    /// <param name="buildCorsPolicy">Customize the CORS policy. </param>
    public static IWebHostBuilder AddAllActuators(this IWebHostBuilder hostBuilder, Action<IEndpointConventionBuilder> configureEndpoints = null, MediaTypeVersion mediaTypeVersion = MediaTypeVersion.V2, Action<CorsPolicyBuilder> buildCorsPolicy = null)
        => hostBuilder.AddManagementPort()
            .ConfigureLogging(builder => builder.AddDynamicConsole())
            .ConfigureServices((context, collection) =>
            {
                collection.AddAllActuators(context.Configuration, mediaTypeVersion, buildCorsPolicy);
                collection.ActivateActuatorEndpoints(configureEndpoints);
            });

    /// <summary>
    /// Adds Wavefront to the application
    /// </summary>
    /// <param name="hostBuilder">Your HostBuilder</param>
    [Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
    public static IWebHostBuilder AddWavefrontMetrics(this IWebHostBuilder hostBuilder)
        => hostBuilder
            .ConfigureServices((context, collection) => collection.AddWavefrontMetrics());

    internal static void GetManagementUrl(this IWebHostBuilder webHostBuilder, out int? httpPort, out int? httpsPort)
    {
        string portSetting = webHostBuilder.GetSetting(ManagementPortKey);
        string sslSetting = webHostBuilder.GetSetting(ManagementSSLKey);

        httpPort = httpsPort = null;
        if (string.IsNullOrEmpty(portSetting))
        {
            IConfiguration config = GetConfiguration(); // try reading directly from appsettings.json
            portSetting = config?[ManagementPortKey];
            sslSetting = config?[ManagementSSLKey];
        }

        if (int.TryParse(portSetting, out int intManagementPort))
        {
            bool.TryParse(sslSetting, out var sslEnabled);
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
        webhostBuilder.GetManagementUrl(out var httpPort, out var httpsPort);
        if (httpPort.HasValue || httpsPort.HasValue)
        {
            webhostBuilder.UseCloudHosting(httpPort, httpsPort);
        }

        return webhostBuilder;
    }

    private static IConfiguration GetConfiguration()
    {
        IConfiguration config = null;

        try
        {
            config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        }
        catch (Exception)
        {
            // Not much we can do ...
        }

        return config;
    }
}
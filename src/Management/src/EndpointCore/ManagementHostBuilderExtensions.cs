// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
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
using System;

namespace Steeltoe.Management.Endpoint
{
    public static class ManagementHostBuilderExtensions
    {
        /// <summary>
        /// Adds the Database Migrations actuator to the application
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        public static IHostBuilder AddDbMigrationsActuator(this IHostBuilder hostBuilder)
        {
            return hostBuilder
                .ConfigureServices((context, collection) =>
                {
                    collection.AddDbMigrationsActuator(context.Configuration);
                    collection.AddActuatorStartupFilter();
                });
        }

        /// <summary>
        /// Adds the Environment actuator to the application
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        public static IHostBuilder AddEnvActuator(this IHostBuilder hostBuilder)
        {
            return hostBuilder
                .ConfigureServices((context, collection) =>
                {
                    collection.AddEnvActuator(context.Configuration);
                    collection.AddActuatorStartupFilter();
                });
        }

        /// <summary>
        /// Adds the Health actuator to the application
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        public static IHostBuilder AddHealthActuator(this IHostBuilder hostBuilder)
        {
            return hostBuilder
                .ConfigureServices((context, collection) =>
                {
                    collection.AddHealthActuator(context.Configuration);
                    collection.AddActuatorStartupFilter();
                });
        }

        /// <summary>
        /// Adds the Health actuator to the application
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        /// <param name="contributors">Types that contribute to the overall health of the app</param>
        public static IHostBuilder AddHealthActuator(this IHostBuilder hostBuilder, Type[] contributors)
        {
            return hostBuilder
                .ConfigureServices((context, collection) =>
                {
                    collection.AddHealthActuator(context.Configuration, contributors);
                    collection.AddActuatorStartupFilter();
                });
        }

        /// <summary>
        /// Adds the Health actuator to the application
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        /// <param name="aggregator">Custom health aggregator</param>
        /// <param name="contributors">Types that contribute to the overall health of the app</param>
        public static IHostBuilder AddHealthActuator(this IHostBuilder hostBuilder, IHealthAggregator aggregator, Type[] contributors)
        {
            return hostBuilder
                .ConfigureServices((context, collection) =>
                {
                    collection.AddHealthActuator(context.Configuration, aggregator, contributors);
                    collection.AddActuatorStartupFilter();
                });
        }

        /// <summary>
        /// Adds the HeapDump actuator to the application
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        public static IHostBuilder AddHeapDumpActuator(this IHostBuilder hostBuilder)
        {
            return hostBuilder
                .ConfigureServices((context, collection) =>
                {
                    collection.AddHeapDumpActuator(context.Configuration);
                    collection.AddActuatorStartupFilter();
                });
        }

        /// <summary>
        /// Adds the Hypermedia actuator to the application
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        public static IHostBuilder AddHypermediaActuator(this IHostBuilder hostBuilder)
        {
            return hostBuilder
                .ConfigureServices((context, collection) =>
                {
                    collection.AddHypermediaActuator(context.Configuration);
                    collection.AddActuatorStartupFilter();
                });
        }

        /// <summary>
        /// Adds the Info actuator to the application
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        public static IHostBuilder AddInfoActuator(this IHostBuilder hostBuilder)
        {
            return hostBuilder
                .ConfigureServices((context, collection) =>
                {
                    collection.AddInfoActuator(context.Configuration);
                    collection.AddActuatorStartupFilter();
                });
        }

        /// <summary>
        /// Adds the Info actuator to the application
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        /// <param name="contributors">Contributors to application information</param>
        public static IHostBuilder AddInfoActuator(this IHostBuilder hostBuilder, IInfoContributor[] contributors)
        {
            return hostBuilder
                .ConfigureServices((context, collection) =>
                {
                    collection.AddInfoActuator(context.Configuration, contributors);
                    collection.AddActuatorStartupFilter();
                });
        }

        /// <summary>
        /// Adds the Loggers actuator to the application
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        public static IHostBuilder AddLoggersActuator(this IHostBuilder hostBuilder)
        {
            return hostBuilder
                .AddDynamicLogging()
                .ConfigureServices((context, collection) =>
                {
                    collection.AddLoggersActuator(context.Configuration);
                    collection.AddActuatorStartupFilter();
                });
        }

        /// <summary>
        /// Adds the Mappings actuator to the application
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        public static IHostBuilder AddMappingsActuator(this IHostBuilder hostBuilder)
        {
            return hostBuilder
                .ConfigureServices((context, collection) =>
                {
                    collection.AddMappingsActuator(context.Configuration);
                    collection.AddActuatorStartupFilter();
                });
        }

        /// <summary>
        /// Adds the Metrics actuator to the application
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        public static IHostBuilder AddMetricsActuator(this IHostBuilder hostBuilder)
        {
            return hostBuilder
                .ConfigureServices((context, collection) =>
                {
                    collection.AddMetricsActuator(context.Configuration);
                    collection.AddActuatorStartupFilter();
                });
        }

        /// <summary>
        /// Adds the Refresh actuator to the application
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        public static IHostBuilder AddRefreshActuator(this IHostBuilder hostBuilder)
        {
            return hostBuilder
                .ConfigureServices((context, collection) =>
                {
                    collection.AddRefreshActuator(context.Configuration);
                    collection.AddActuatorStartupFilter();
                });
        }

        /// <summary>
        /// Adds the ThreadDump actuator to the application
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        /// <param name="mediaTypeVersion">Specify the media type version to use in the response</param>
        public static IHostBuilder AddThreadDumpActuator(this IHostBuilder hostBuilder, MediaTypeVersion mediaTypeVersion = MediaTypeVersion.V2)
        {
            return hostBuilder
                .ConfigureServices((context, collection) =>
                {
                    collection.AddThreadDumpActuator(context.Configuration, mediaTypeVersion);
                    collection.AddActuatorStartupFilter();
                });
        }

        /// <summary>
        /// Adds the Trace actuator to the application
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        /// <param name="mediaTypeVersion">Specify the media type version to use in the response</param>
        public static IHostBuilder AddTraceActuator(this IHostBuilder hostBuilder, MediaTypeVersion mediaTypeVersion = MediaTypeVersion.V2)
        {
            return hostBuilder
                .ConfigureServices((context, collection) =>
                {
                    collection.AddTraceActuator(context.Configuration, mediaTypeVersion);
                    collection.AddActuatorStartupFilter();
                });
        }

        /// <summary>
        /// Adds the Cloud Foundry actuator to the application
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        public static IHostBuilder AddCloudFoundryActuator(this IHostBuilder hostBuilder)
        {
            return hostBuilder
                .ConfigureServices((context, collection) =>
                {
                    collection.AddCloudFoundryActuator(context.Configuration);
                    collection.AddActuatorEndpointEntry<CloudFoundryEndpoint>();
                    collection.AddActuatorStartupFilter();
                });
        }

        public static IHostBuilder AddAllActuators(this IHostBuilder hostBuilder, Action<IEndpointConventionBuilder> configureEndpoints = null, MediaTypeVersion mediaTypeVersion = MediaTypeVersion.V2)
        {
            return hostBuilder
                .AddDynamicLogging()
                .ConfigureServices((context, collection) =>
                {
                    collection.AddAllActuators(context.Configuration, mediaTypeVersion);
                    collection.AddActuatorStartupFilter(configureEndpoints);
                });
        }
    }
}

// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Extensions.Logging.DynamicLogger;
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
                    collection.AddTransient<IStartupFilter, DbMigrationsStartupFilter>();
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
                    collection.AddTransient<IStartupFilter, EnvStartupFilter>();
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
                    collection.AddTransient<IStartupFilter, HealthStartupFilter>();
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
                    collection.AddTransient<IStartupFilter, HealthStartupFilter>();
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
                    collection.AddTransient<IStartupFilter, HealthStartupFilter>();
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
                    collection.AddTransient<IStartupFilter, HeapDumpStartupFilter>();
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
                    collection.AddTransient<IStartupFilter, HypermediaStartupFilter>();
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
                    collection.AddTransient<IStartupFilter, InfoStartupFilter>();
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
                    collection.AddTransient<IStartupFilter, InfoStartupFilter>();
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
                    collection.AddTransient<IStartupFilter, LoggersStartupFilter>();
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
                    collection.AddTransient<IStartupFilter, MappingsStartupFilter>();
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
                    collection.AddTransient<IStartupFilter, MetricsStartupFilter>();
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
                    collection.AddTransient<IStartupFilter, RefreshStartupFilter>();
                });
        }

        /// <summary>
        /// Adds the ThreadDump actuator to the application
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        /// <param name="mediaTypeVersion">Specify the media type version to use in the response</param>
        public static IHostBuilder AddThreadDumpActuator(this IHostBuilder hostBuilder, MediaTypeVersion mediaTypeVersion = MediaTypeVersion.V1)
        {
            return hostBuilder
                .ConfigureServices((context, collection) =>
                {
                    collection.AddThreadDumpActuator(context.Configuration, mediaTypeVersion);
                    collection.AddTransient<IStartupFilter, ThreadDumpStartupFilter>();
                });
        }

        /// <summary>
        /// Adds the Trace actuator to the application
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        /// <param name="mediaTypeVersion">Specify the media type version to use in the response</param>
        public static IHostBuilder AddTraceActuator(this IHostBuilder hostBuilder, MediaTypeVersion mediaTypeVersion = MediaTypeVersion.V1)
        {
            return hostBuilder
                .ConfigureServices((context, collection) =>
                {
                    collection.AddTraceActuator(context.Configuration, mediaTypeVersion);
                    collection.AddTransient<IStartupFilter, TraceStartupFilter>();
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
                    collection.AddTransient<IStartupFilter, CloudFoundryActuatorStartupFilter>();
                });
        }
    }
}

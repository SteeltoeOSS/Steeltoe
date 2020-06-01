// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.DbMigrations;
using Steeltoe.Management.Endpoint.Env;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Mappings;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Refresh;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;

namespace Steeltoe.Management.EndpointCore.Test.ContentNegotiation
{
    public static class TestStartupExtensions
    {
        public enum EndpointNames
        {
            Cloudfoundry,
            Hypermedia,
            Info,
            Metrics,
            Loggers,
            Health,
            Trace,
            DbMigrations,
            Env,
            Mappings,
            Refresh,
            ThreadDump
        }

        public static IWebHostBuilder StartupByEpName(this IWebHostBuilder builder, EndpointNames endpointName)
        {
            switch (endpointName)
            {
                case EndpointNames.Cloudfoundry: return builder.UseStartup<CloudFoundryStartup>();
                case EndpointNames.Hypermedia: return builder.UseStartup<HyperMediaStartup>();
                case EndpointNames.Info: return builder.UseStartup<InfoStartup>();
                case EndpointNames.Metrics: return builder.UseStartup<MetricsStartup>();
                case EndpointNames.Loggers: return builder.UseStartup<LoggersStartup>();
                case EndpointNames.Health: return builder.UseStartup<HealthStartup>();
                case EndpointNames.Trace: return builder.UseStartup<TraceStartup>();
                case EndpointNames.DbMigrations: return builder.UseStartup<DbMigrationsStartup>();
                case EndpointNames.Env: return builder.UseStartup<EnvStartup>();
                case EndpointNames.Mappings: return builder.UseStartup<MappingsStartup>();
                case EndpointNames.Refresh: return builder.UseStartup<RefreshStartup>();
                case EndpointNames.ThreadDump: return builder.UseStartup<ThreadDumpStartup>();
            }

            return builder;
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    public class CloudFoundryStartup
#pragma warning restore SA1402 // File may only contain a single class
    {
        public CloudFoundryStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services) =>
            services.AddCloudFoundryActuator(Configuration);

        public void Configure(IApplicationBuilder app) =>
            app.UseCloudFoundryActuator();
    }

#pragma warning disable SA1402 // File may only contain a single class
    public class HyperMediaStartup
#pragma warning restore SA1402 // File may only contain a single class
    {
        public HyperMediaStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services) =>
            services.AddHypermediaActuator(Configuration);

        public void Configure(IApplicationBuilder app) =>
            app.UseHypermediaActuator();
    }

#pragma warning disable SA1402 // File may only contain a single class
    public class InfoStartup
#pragma warning restore SA1402 // File may only contain a single class
    {
        public InfoStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHypermediaActuator(Configuration);
            services.AddInfoActuator(Configuration);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseHypermediaActuator();
            app.UseInfoActuator();
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    public class MetricsStartup
#pragma warning restore SA1402 // File may only contain a single class
    {
        public MetricsStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHypermediaActuator(Configuration);
            services.AddMetricsActuator(Configuration);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseHypermediaActuator();
            app.UseMetricsActuator();
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    public class LoggersStartup
#pragma warning restore SA1402 // File may only contain a single class
    {
        public LoggersStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHypermediaActuator(Configuration);
            services.AddLoggersActuator(Configuration);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseHypermediaActuator();
            app.UseLoggersActuator();
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    public class HealthStartup
#pragma warning restore SA1402 // File may only contain a single class
    {
        public HealthStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHypermediaActuator(Configuration);
            services.AddHealthActuator(Configuration);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseHypermediaActuator();
            app.UseHealthActuator();
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    public class TraceStartup
#pragma warning restore SA1402 // File may only contain a single class
    {
        public TraceStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHypermediaActuator(Configuration);
            services.AddTraceActuator(Configuration);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseHypermediaActuator();
            app.UseTraceActuator();
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    public class DbMigrationsStartup
#pragma warning restore SA1402 // File may only contain a single class
    {
        public DbMigrationsStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHypermediaActuator(Configuration);
            services.AddDbMigrationsActuator(Configuration);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseHypermediaActuator();
            app.UseDbMigrationsActuator();
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    public class EnvStartup
#pragma warning restore SA1402 // File may only contain a single class
    {
        public EnvStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHypermediaActuator(Configuration);
            services.AddEnvActuator(Configuration);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseHypermediaActuator();
            app.UseEnvActuator();
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    public class MappingsStartup
#pragma warning restore SA1402 // File may only contain a single class
    {
        public MappingsStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHypermediaActuator(Configuration);
            services.AddMappingsActuator(Configuration);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseHypermediaActuator();
            app.UseMappingsActuator();
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    public class RefreshStartup
#pragma warning restore SA1402 // File may only contain a single class
    {
        public RefreshStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHypermediaActuator(Configuration);
            services.AddRefreshActuator(Configuration);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseHypermediaActuator();
            app.UseRefreshActuator();
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    public class ThreadDumpStartup
#pragma warning restore SA1402 // File may only contain a single class
    {
        public ThreadDumpStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHypermediaActuator(Configuration);
            services.AddThreadDumpActuator(Configuration);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseHypermediaActuator();
            app.UseThreadDumpActuator();
        }
    }
}

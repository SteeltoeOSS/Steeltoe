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

namespace Steeltoe.Management.Endpoint.ContentNegotiation.Test;

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
        return endpointName switch
        {
            EndpointNames.Cloudfoundry => builder.UseStartup<CloudFoundryStartup>(),
            EndpointNames.Hypermedia => builder.UseStartup<HyperMediaStartup>(),
            EndpointNames.Info => builder.UseStartup<InfoStartup>(),
            EndpointNames.Metrics => builder.UseStartup<MetricsStartup>(),
            EndpointNames.Loggers => builder.UseStartup<LoggersStartup>(),
            EndpointNames.Health => builder.UseStartup<HealthStartup>(),
            EndpointNames.Trace => builder.UseStartup<TraceStartup>(),
            EndpointNames.DbMigrations => builder.UseStartup<DbMigrationsStartup>(),
            EndpointNames.Env => builder.UseStartup<EnvStartup>(),
            EndpointNames.Mappings => builder.UseStartup<MappingsStartup>(),
            EndpointNames.Refresh => builder.UseStartup<RefreshStartup>(),
            EndpointNames.ThreadDump => builder.UseStartup<ThreadDumpStartup>(),
            _ => builder,
        };
    }
}

public class CloudFoundryStartup
{
    public CloudFoundryStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; set; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddCloudFoundryActuator(Configuration);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.Map<CloudFoundryEndpoint>();
        });
    }
}

public class HyperMediaStartup
{
    public HyperMediaStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; set; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddHypermediaActuator(Configuration);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.Map<ActuatorEndpoint>();
        });
    }
}

public class InfoStartup
{
    public InfoStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddHypermediaActuator(Configuration);
        services.AddInfoActuator(Configuration);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.Map<ActuatorEndpoint>();
            endpoints.Map<InfoEndpoint>();
        });
    }
}

public class MetricsStartup
{
    public MetricsStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddHypermediaActuator(Configuration);
        services.AddMetricsActuator(Configuration);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.Map<ActuatorEndpoint>();
            endpoints.Map<MetricsEndpoint>();
        });
    }
}

public class LoggersStartup
{
    public LoggersStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddHypermediaActuator(Configuration);
        services.AddLoggersActuator(Configuration);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.Map<ActuatorEndpoint>();
            endpoints.Map<LoggersEndpoint>();
        });
    }
}

public class HealthStartup
{
    public HealthStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddHypermediaActuator(Configuration);
        services.AddHealthActuator(Configuration);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.Map<ActuatorEndpoint>();
            endpoints.Map<HealthEndpointCore>();
        });
    }
}

public class TraceStartup
{
    public TraceStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddHypermediaActuator(Configuration);
        services.AddTraceActuator(Configuration);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.Map<ActuatorEndpoint>();
            endpoints.Map<HttpTraceEndpoint>();
        });
    }
}

public class DbMigrationsStartup
{
    public DbMigrationsStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddHypermediaActuator(Configuration);
        services.AddDbMigrationsActuator(Configuration);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.Map<ActuatorEndpoint>();
            endpoints.Map<DbMigrationsEndpoint>();
        });
    }
}

public class EnvStartup
{
    public EnvStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddHypermediaActuator(Configuration);
        services.AddEnvActuator(Configuration);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.Map<ActuatorEndpoint>();
            endpoints.Map<EnvEndpoint>();
        });
    }
}

public class MappingsStartup
{
    public MappingsStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddHypermediaActuator(Configuration);
        services.AddMappingsActuator(Configuration);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.Map<ActuatorEndpoint>();
            endpoints.Map<MappingsEndpoint>();
        });
    }
}

public class RefreshStartup
{
    public RefreshStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddHypermediaActuator(Configuration);
        services.AddRefreshActuator(Configuration);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.Map<ActuatorEndpoint>();
            endpoints.Map<RefreshEndpoint>();
        });
    }
}

public class ThreadDumpStartup
{
    public ThreadDumpStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddHypermediaActuator(Configuration);
        services.AddThreadDumpActuator(Configuration);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.Map<ActuatorEndpoint>();
            endpoints.Map<ThreadDumpEndpoint>();
        });
    }
}

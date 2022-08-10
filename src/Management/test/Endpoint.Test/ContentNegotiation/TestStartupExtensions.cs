// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;

namespace Steeltoe.Management.Endpoint.ContentNegotiation.Test;

public static class TestStartupExtensions
{
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
            _ => builder
        };
    }

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
}

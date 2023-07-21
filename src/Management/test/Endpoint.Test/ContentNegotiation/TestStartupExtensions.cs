// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;

namespace Steeltoe.Management.Endpoint.Test.ContentNegotiation;

public static class TestStartupExtensions
{
    public static IWebHostBuilder UseStartupForEndpoint(this IWebHostBuilder builder, EndpointName endpointName)
    {
        return endpointName switch
        {
            EndpointName.Cloudfoundry => builder.UseStartup<CloudFoundryStartup>(),
            EndpointName.Hypermedia => builder.UseStartup<HyperMediaStartup>(),
            EndpointName.Info => builder.UseStartup<InfoStartup>(),
            EndpointName.Metrics => builder.UseStartup<MetricsStartup>(),
            EndpointName.Loggers => builder.UseStartup<LoggersStartup>(),
            EndpointName.Health => builder.UseStartup<HealthStartup>(),
            EndpointName.Trace => builder.UseStartup<TraceStartup>(),
            EndpointName.DbMigrations => builder.UseStartup<DbMigrationsStartup>(),
            EndpointName.Environment => builder.UseStartup<EnvironmentStartup>(),
            EndpointName.Mappings => builder.UseStartup<MappingsStartup>(),
            EndpointName.Refresh => builder.UseStartup<RefreshStartup>(),
            EndpointName.ThreadDump => builder.UseStartup<ThreadDumpStartup>(),
            _ => builder
        };
    }

    public enum EndpointName
    {
        Cloudfoundry,
        Hypermedia,
        Info,
        Metrics,
        Loggers,
        Health,
        Trace,
        DbMigrations,
        Environment,
        Mappings,
        Refresh,
        ThreadDump
    }
}

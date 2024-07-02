// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Aspire;
using Steeltoe.Management.Diagnostics;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.DbMigrations;
using Steeltoe.Management.Endpoint.Environment;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Health.Contributor;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.Refresh;
using Steeltoe.Management.Endpoint.RouteMappings;
using Steeltoe.Management.Endpoint.Services;
using Steeltoe.Management.Endpoint.SpringBootAdminClient;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;
using Steeltoe.Management.Endpoint.Web.Hypermedia;

[assembly: ConfigurationSchema("Spring:Application:Name", typeof(string))]
[assembly: ConfigurationSchema("Spring:Boot:Admin:Client", typeof(SpringBootAdminClientOptions))]
[assembly: ConfigurationSchema("Management:CloudFoundry:Enabled", typeof(bool))]
[assembly: ConfigurationSchema("Management:Endpoints", typeof(ManagementOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:SslEnabled", typeof(bool))]
[assembly: ConfigurationSchema("Management:Endpoints:CloudFoundry", typeof(CloudFoundryEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:DbMigrations", typeof(DbMigrationsEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:Env", typeof(EnvironmentEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:Health", typeof(HealthEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:Health:DiskSpace", typeof(DiskSpaceContributorOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:HeapDump", typeof(HeapDumpEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:Info", typeof(InfoEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:Loggers", typeof(LoggersEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:Metrics", typeof(MetricsEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:Refresh", typeof(RefreshEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:Mappings", typeof(RouteMappingsEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:Services", typeof(ServicesEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:Dump", typeof(ThreadDumpEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:Trace", typeof(TraceEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:HttpTrace", typeof(TraceEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:Actuator", typeof(HypermediaEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:Actuator:Exposure", typeof(Exposure))]
[assembly: ConfigurationSchema("Management:Endpoints:Web:Exposure", typeof(Exposure))]
[assembly: ConfigurationSchema("Management:Metrics:Observer", typeof(MetricsObserverOptions))]
[assembly: LoggingCategories("Steeltoe", "Steeltoe.Management", "Steeltoe.Management.Endpoint")]

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("Steeltoe.Bootstrap.AutoConfiguration")]
[assembly: InternalsVisibleTo("Steeltoe.Discovery.Eureka")]
[assembly: InternalsVisibleTo("Steeltoe.Management.Endpoint.Test")]
[assembly: InternalsVisibleTo("Steeltoe.Management.Prometheus")]
[assembly: InternalsVisibleTo("Steeltoe.Management.Wavefront")]
[assembly: InternalsVisibleTo("Steeltoe.Management.Tracing")]
[assembly: InternalsVisibleTo("Steeltoe.Management.Wavefront.Test")]

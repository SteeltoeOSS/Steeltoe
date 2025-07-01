// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Aspire;
using Steeltoe.Common.Configuration;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Actuators.DbMigrations;
using Steeltoe.Management.Endpoint.Actuators.Environment;
using Steeltoe.Management.Endpoint.Actuators.Health;
using Steeltoe.Management.Endpoint.Actuators.Health.Contributors;
using Steeltoe.Management.Endpoint.Actuators.HeapDump;
using Steeltoe.Management.Endpoint.Actuators.HttpExchanges;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Endpoint.Actuators.Info;
using Steeltoe.Management.Endpoint.Actuators.Loggers;
using Steeltoe.Management.Endpoint.Actuators.Refresh;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings;
using Steeltoe.Management.Endpoint.Actuators.Services;
using Steeltoe.Management.Endpoint.Actuators.ThreadDump;
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.SpringBootAdminClient;

[assembly: ConfigurationSchema("Spring:Application", typeof(SpringApplicationSettings))]
[assembly: ConfigurationSchema("Spring:Boot:Admin:Client", typeof(SpringBootAdminClientOptions))]
[assembly: ConfigurationSchema("Management:CloudFoundry:Enabled", typeof(bool))]
[assembly: ConfigurationSchema("Management:Endpoints", typeof(ManagementOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:CloudFoundry", typeof(CloudFoundryEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:DbMigrations", typeof(DbMigrationsEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:Env", typeof(EnvironmentEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:Health", typeof(HealthEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:Health:DiskSpace", typeof(DiskSpaceContributorOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:Health:Ping", typeof(PingContributorOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:Health:Liveness", typeof(LivenessStateContributorOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:Health:Readiness", typeof(ReadinessStateContributorOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:HeapDump", typeof(HeapDumpEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:Info", typeof(InfoEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:Loggers", typeof(LoggersEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:Refresh", typeof(RefreshEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:Mappings", typeof(RouteMappingsEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:Services", typeof(ServicesEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:ThreadDump", typeof(ThreadDumpEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:HttpExchanges", typeof(HttpExchangesEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:Actuator", typeof(HypermediaEndpointOptions))]
[assembly: ConfigurationSchema("Management:Endpoints:Actuator:Exposure", typeof(Exposure))]
[assembly: ConfigurationSchema("Management:Endpoints:Web:Exposure", typeof(Exposure))]
[assembly: LoggingCategories("Steeltoe", "Steeltoe.Management", "Steeltoe.Management.Endpoint")]

[assembly: InternalsVisibleTo("Steeltoe.Bootstrap.AutoConfiguration")]
[assembly: InternalsVisibleTo("Steeltoe.Bootstrap.AutoConfiguration.Test")]
[assembly: InternalsVisibleTo("Steeltoe.Discovery.Eureka")]
[assembly: InternalsVisibleTo("Steeltoe.Management.Endpoint.Test")]
[assembly: InternalsVisibleTo("Steeltoe.Management.Prometheus")]
[assembly: InternalsVisibleTo("Steeltoe.Management.Tracing")]

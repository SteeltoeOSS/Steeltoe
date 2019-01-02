// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNet.TelemetryCorrelation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Env;
using Steeltoe.Management.Endpoint.Handler;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Health.Contributor;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Info.Contributor;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Mappings;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Metrics.Observer;
using Steeltoe.Management.Endpoint.Module;
using Steeltoe.Management.Endpoint.Refresh;
using Steeltoe.Management.Endpoint.Security;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;
using Steeltoe.Management.Endpoint.Trace.Observer;
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Http.Description;

[assembly: PreApplicationStartMethod(typeof(Steeltoe.Management.Endpoint.ActuatorConfigurator), "ConfigureModules")]

namespace Steeltoe.Management.Endpoint
{
    public static class ActuatorConfigurator
    {
        public static ILoggerFactory LoggerFactory { get; set; }

        public static void UseCloudFoundryActuators(IConfiguration configuration, ILoggerProvider dynamicLogger, IEnumerable<IHealthContributor> healthContributors = null, IApiExplorer apiExplorer = null, ILoggerFactory loggerFactory = null)
        {
            UseCloudFoundrySecurity(configuration, null, loggerFactory);
            UseEndpointSecurity(configuration, null, loggerFactory);
            UseCloudFoundryActuator(configuration, loggerFactory);
            UseHealthActuator(configuration, null, healthContributors, loggerFactory);
            UseHeapDumpActuator(configuration, null, loggerFactory);
            UseThreadDumpActuator(configuration, null, loggerFactory);
            UseInfoActuator(configuration, null, loggerFactory);
            UseLoggerActuator(configuration, dynamicLogger, loggerFactory);
            UseTraceActuator(configuration, null, loggerFactory);
            UseMappingsActuator(configuration, apiExplorer, loggerFactory);
        }

        public static void UseAllActuators(IConfiguration configuration, ILoggerProvider dynamicLogger, IEnumerable<IHealthContributor> healthContributors = null, IApiExplorer apiExplorer = null, ILoggerFactory loggerFactory = null)
        {
            UseCloudFoundryActuators(configuration, dynamicLogger, healthContributors, apiExplorer, loggerFactory);
            UseEnvActuator(configuration, null, loggerFactory);
            UseRefreshActuator(configuration, loggerFactory);
            UseMetricsActuator(configuration, loggerFactory);
        }

        public static void ConfigureModules()
        {
            DynamicModuleUtility.RegisterModule(typeof(TelemetryCorrelationHttpModule));
            DynamicModuleUtility.RegisterModule(typeof(ActuatorModule));
        }

        public static void UseCloudFoundrySecurity(IConfiguration configuration, ISecurityService securityService = null, ILoggerFactory loggerFactory = null)
        {
            SecurityServices.Add(new CloudFoundrySecurity(new CloudFoundryOptions(configuration), CreateLogger<CloudFoundrySecurity>(loggerFactory)));
        }

        public static void UseEndpointSecurity(IConfiguration configuration, ISecurityService securityService = null, ILoggerFactory loggerFactory = null)
        {
            SecurityServices.Add(new EndpointSecurity(new CloudFoundryOptions(configuration), CreateLogger<EndpointSecurity>(loggerFactory)));
        }

        public static void UseCloudFoundryActuator(IConfiguration configuration, ILoggerFactory loggerFactory = null)
        {
            var options = new CloudFoundryOptions(configuration);
            var ep = new CloudFoundryEndpoint(options, CreateLogger<CloudFoundryEndpoint>(loggerFactory));
            var handler = new CloudFoundryHandler(ep, SecurityServices, CreateLogger<CloudFoundryHandler>(loggerFactory));
            ConfiguredHandlers.Add(handler);
            var handler2 = new CloudFoundryCorsHandler(options, SecurityServices, CreateLogger<CloudFoundryCorsHandler>(loggerFactory));
            ConfiguredHandlers.Add(handler2);
        }

        public static void UseHeapDumpActuator(IConfiguration configuration, IHeapDumper heapDumper = null, ILoggerFactory loggerFactory = null)
        {
            var options = new HeapDumpOptions(configuration);
            heapDumper = heapDumper ?? new HeapDumper(options);
            var ep = new HeapDumpEndpoint(options, heapDumper, CreateLogger<HeapDumpEndpoint>(loggerFactory));
            var handler = new HeapDumpHandler(ep, SecurityServices, CreateLogger<HeapDumpHandler>(loggerFactory));
            ConfiguredHandlers.Add(handler);
        }

        public static void UseHealthActuator(IConfiguration configuration, IHealthAggregator healthAggregator = null, IEnumerable<IHealthContributor> contributors = null, ILoggerFactory loggerFactory = null)
        {
            var options = new HealthOptions(configuration);
            healthAggregator = healthAggregator ?? new DefaultHealthAggregator();
            contributors = contributors ?? new List<IHealthContributor>() { new DiskSpaceContributor(new DiskSpaceContributorOptions(configuration)) };
            var ep = new HealthEndpoint(options, healthAggregator, contributors, CreateLogger<HealthEndpoint>(loggerFactory));
            var handler = new HealthHandler(ep, SecurityServices, CreateLogger<HealthHandler>(loggerFactory));
            ConfiguredHandlers.Add(handler);
        }

        public static void UseInfoActuator(IConfiguration configuration, IEnumerable<IInfoContributor> contributors = null, ILoggerFactory loggerFactory = null)
        {
            var options = new InfoOptions(configuration);
            contributors = contributors ?? new List<IInfoContributor>() { new GitInfoContributor(), new AppSettingsInfoContributor(configuration) };
            var ep = new InfoEndpoint(options, contributors, CreateLogger<InfoEndpoint>(loggerFactory));
            var handler = new InfoHandler(ep, SecurityServices, CreateLogger<InfoHandler>(loggerFactory));
            ConfiguredHandlers.Add(handler);
        }

        public static void UseLoggerActuator(IConfiguration configuration, ILoggerProvider loggerProvider, ILoggerFactory loggerFactory = null)
        {
            var ep = new LoggersEndpoint(new LoggersOptions(configuration), loggerProvider as IDynamicLoggerProvider, CreateLogger<LoggersEndpoint>(loggerFactory));
            var handler = new LoggersHandler(ep, SecurityServices, CreateLogger<LoggersHandler>(loggerFactory));
            ConfiguredHandlers.Add(handler);
        }

        public static void UseThreadDumpActuator(IConfiguration configuration, IThreadDumper threadDumper = null, ILoggerFactory loggerFactory = null)
        {
            var options = new ThreadDumpOptions(configuration);
            threadDumper = threadDumper ?? new ThreadDumper(options);
            var ep = new ThreadDumpEndpoint(options, threadDumper, CreateLogger<ThreadDumpEndpoint>(loggerFactory));
            var handler = new ThreadDumpHandler(ep, SecurityServices, CreateLogger<ThreadDumpHandler>(loggerFactory));
            ConfiguredHandlers.Add(handler);
        }

        public static void UseTraceActuator(IConfiguration configuration, ITraceRepository traceRepository = null, ILoggerFactory loggerFactory = null)
        {
            var options = new TraceOptions(configuration);
            traceRepository = traceRepository ?? new TraceDiagnosticObserver(options, CreateLogger<TraceDiagnosticObserver>(loggerFactory));
            DiagnosticsManager.Instance.Observers.Add((IDiagnosticObserver)traceRepository);
            var ep = new TraceEndpoint(options, traceRepository, CreateLogger<TraceEndpoint>(loggerFactory));
            var handler = new TraceHandler(ep, SecurityServices, CreateLogger<TraceHandler>(loggerFactory));
            ConfiguredHandlers.Add(handler);
        }

        public static void UseRefreshActuator(IConfiguration configuration, ILoggerFactory loggerFactory = null)
        {
            var ep = new RefreshEndpoint(new RefreshOptions(configuration), configuration, CreateLogger<RefreshEndpoint>(loggerFactory));
            var handler = new RefreshHandler(ep, SecurityServices, CreateLogger<RefreshHandler>(loggerFactory));
            ConfiguredHandlers.Add(handler);
        }

        public static void UseEnvActuator(IConfiguration configuration, IHostingEnvironment hostingEnvironment = null, ILoggerFactory loggerFactory = null)
        {
            var options = new EnvOptions(configuration);
            hostingEnvironment = hostingEnvironment ?? new DefaultHostingEnvironment("development");
            var ep = new EnvEndpoint(options, configuration, hostingEnvironment, CreateLogger<EnvEndpoint>(loggerFactory));
            var handler = new EnvHandler(ep, SecurityServices, CreateLogger<EnvHandler>(loggerFactory));
            ConfiguredHandlers.Add(handler);
        }

        public static void UseMetricsActuator(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            var options = new MetricsOptions(configuration);

            var hostObserver = new AspNetHostingObserver(options, OpenCensusStats.Instance, OpenCensusTags.Instance, CreateLogger<AspNetHostingObserver>(loggerFactory));
            var clrObserver = new CLRRuntimeObserver(options, OpenCensusStats.Instance, OpenCensusTags.Instance, CreateLogger<CLRRuntimeObserver>(loggerFactory));
            DiagnosticsManager.Instance.Observers.Add((IDiagnosticObserver)hostObserver);
            DiagnosticsManager.Instance.Observers.Add((IDiagnosticObserver)clrObserver);

            var clrSource = new CLRRuntimeSource();
            DiagnosticsManager.Instance.Sources.Add(clrSource);
            var ep = new MetricsEndpoint(options, OpenCensusStats.Instance, CreateLogger<MetricsEndpoint>(loggerFactory));
            var handler = new MetricsHandler(ep, SecurityServices, CreateLogger<MetricsHandler>(loggerFactory));
            ConfiguredHandlers.Add(handler);
        }

        public static void UseMappingsActuator(IConfiguration configuration, IApiExplorer apiExplorer = null, ILoggerFactory loggerFactory = null)
        {
            var options = new MappingsOptions(configuration);
            var handler = new MappingsHandler(options, SecurityServices, apiExplorer, CreateLogger<MappingsHandler>(loggerFactory));
            ConfiguredHandlers.Add(handler);
        }

        public static IList<IActuatorHandler> ConfiguredHandlers { get; } = new List<IActuatorHandler>();

        internal static List<ISecurityService> SecurityServices { get; } = new List<ISecurityService>();

        private static ILogger<T> CreateLogger<T>(ILoggerFactory loggerFactory)
        {
            return loggerFactory != null ? loggerFactory.CreateLogger<T>() : LoggerFactory?.CreateLogger<T>();
        }

        public class DefaultHostingEnvironment : IHostingEnvironment
        {
            private readonly string profile;

            public DefaultHostingEnvironment(string profile)
            {
                this.profile = profile;
            }

            public string EnvironmentName { get => profile; set => throw new NotImplementedException(); }

            public string ApplicationName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public string ContentRootPath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public IFileProvider ContentRootFileProvider { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        }
    }
}

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

using Microsoft.AspNet.TelemetryCorrelation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using Steeltoe.Common;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Census.Stats;
using Steeltoe.Management.Census.Tags;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Env;
using Steeltoe.Management.Endpoint.Handler;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Health.Contributor;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.Endpoint.Hypermedia;
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
using Steeltoe.Management.Hypermedia;
using System;
using System.Collections.Generic;
using System.Linq;
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
            UseCloudFoundryActuators(configuration, dynamicLogger, MediaTypeVersion.V1, ActuatorContext.ActuatorAndCloudFoundry, healthContributors, apiExplorer);
        }

        public static void UseCloudFoundryActuators(IConfiguration configuration, ILoggerProvider dynamicLogger, MediaTypeVersion version, ActuatorContext context, IEnumerable<IHealthContributor> healthContributors = null, IApiExplorer apiExplorer = null, ILoggerFactory loggerFactory = null)
        {
            if (context != ActuatorContext.Actuator)
            {
                UseCloudFoundrySecurity(configuration, null, loggerFactory);
                UseCloudFoundryActuator(configuration, loggerFactory);
            }
            else
            {
                UseHypermediaActuator(configuration, loggerFactory);
            }

            UseHealthActuator(configuration, null, healthContributors, loggerFactory);
            UseHeapDumpActuator(configuration, null, loggerFactory);
            UseThreadDumpActuator(configuration, version, null, loggerFactory);
            UseInfoActuator(configuration, null, loggerFactory);
            UseLoggerActuator(configuration, dynamicLogger, loggerFactory);
            UseTraceActuator(configuration, version, null, loggerFactory);
            UseMappingsActuator(configuration, apiExplorer, loggerFactory);
        }

        public static void UseAllActuators(IConfiguration configuration, ILoggerProvider dynamicLogger, IEnumerable<IHealthContributor> healthContributors = null, IApiExplorer apiExplorer = null, ILoggerFactory loggerFactory = null)
        {
            UseAllActuators(configuration, dynamicLogger, MediaTypeVersion.V1, ActuatorContext.CloudFoundry, healthContributors, apiExplorer, loggerFactory);
        }

        public static void UseAllActuators(IConfiguration configuration, ILoggerProvider dynamicLogger, MediaTypeVersion version, ActuatorContext context, IEnumerable<IHealthContributor> healthContributors = null, IApiExplorer apiExplorer = null, ILoggerFactory loggerFactory = null)
        {
            UseCloudFoundryActuators(configuration, dynamicLogger, version, context, healthContributors, apiExplorer, loggerFactory);
            UseEnvActuator(configuration, null, loggerFactory);
            UseRefreshActuator(configuration, loggerFactory);
            UseMetricsActuator(configuration, loggerFactory);
        }

        public static void ConfigureModules()
        {
            DynamicModuleUtility.RegisterModule(typeof(TelemetryCorrelationHttpModule));
            DynamicModuleUtility.RegisterModule(typeof(ActuatorModule));
        }

        public static void UseCloudFoundrySecurity(IConfiguration configuration, IEnumerable<ISecurityService> securityServices = null, ILoggerFactory loggerFactory = null)
        {
            var managementOptions = _mgmtOptions.OfType<CloudFoundryManagementOptions>().SingleOrDefault();

            if (managementOptions == null)
            {
                managementOptions = new CloudFoundryManagementOptions(configuration, Platform.IsCloudFoundry);
                _mgmtOptions.Add(managementOptions);
            }

            SecurityServices.Add(new CloudFoundrySecurity(new CloudFoundryEndpointOptions(configuration), managementOptions, CreateLogger<CloudFoundrySecurity>(loggerFactory)));
        }

        public static void UseCloudFoundryActuator(IConfiguration configuration, ILoggerFactory loggerFactory = null)
        {
            var options = new CloudFoundryEndpointOptions(configuration);
            var managementOptions = _mgmtOptions.OfType<CloudFoundryManagementOptions>().SingleOrDefault();

            if (managementOptions == null)
            {
               managementOptions = new CloudFoundryManagementOptions(configuration, Platform.IsCloudFoundry);
               _mgmtOptions.Add(managementOptions);
            }

            managementOptions.EndpointOptions.Add(options);
            var ep = new CloudFoundryEndpoint(options, _mgmtOptions, CreateLogger<CloudFoundryEndpoint>(loggerFactory));
            var handler = new CloudFoundryHandler(ep, SecurityServices, _mgmtOptions, CreateLogger<CloudFoundryHandler>(loggerFactory));
            ConfiguredHandlers.Add(handler);
            var handler2 = new CloudFoundryCorsHandler(options, SecurityServices, _mgmtOptions, CreateLogger<CloudFoundryCorsHandler>(loggerFactory));
            ConfiguredHandlers.Add(handler2);
        }

        public static void UseHypermediaActuator(IConfiguration configuration, ILoggerFactory loggerFactory = null)
        {
            var options = new HypermediaEndpointOptions(configuration);
            var managementOptions = _mgmtOptions.OfType<ActuatorManagementOptions>().SingleOrDefault();

            if (managementOptions == null)
            {
                managementOptions = new ActuatorManagementOptions(configuration);
                _mgmtOptions.Add(managementOptions);
            }

            managementOptions.EndpointOptions.Add(options);

            var ep = new ActuatorEndpoint(options, _mgmtOptions, CreateLogger<ActuatorEndpoint>(loggerFactory));
            var handler = new ActuatorHypermediaHandler(ep, SecurityServices, _mgmtOptions, CreateLogger<ActuatorHypermediaHandler>(loggerFactory));
            ConfiguredHandlers.Add(handler);

            if (ConfiguredHandlers.OfType<CloudFoundryCorsHandler>().Any())
            {
                return;
            }

            var handler2 = new CloudFoundryCorsHandler(options, SecurityServices, _mgmtOptions, CreateLogger<CloudFoundryCorsHandler>(loggerFactory));
            ConfiguredHandlers.Add(handler2);
        }

        public static void UseHeapDumpActuator(IConfiguration configuration, IHeapDumper heapDumper = null, ILoggerFactory loggerFactory = null)
        {
            var options = new HeapDumpEndpointOptions(configuration);
            _mgmtOptions.RegisterEndpointOptions(configuration, options);

            heapDumper = heapDumper ?? new HeapDumper(options);
            var ep = new HeapDumpEndpoint(options, heapDumper, CreateLogger<HeapDumpEndpoint>(loggerFactory));
            var handler = new HeapDumpHandler(ep, SecurityServices, _mgmtOptions, CreateLogger<HeapDumpHandler>(loggerFactory));
            ConfiguredHandlers.Add(handler);
        }

        public static void UseHealthActuator(IConfiguration configuration, IHealthAggregator healthAggregator = null, IEnumerable<IHealthContributor> contributors = null, ILoggerFactory loggerFactory = null)
        {
            var options = new HealthEndpointOptions(configuration);
            _mgmtOptions.RegisterEndpointOptions(configuration, options);
            if (ConfiguredHandlers.OfType<HealthHandler>().Any())
            {
                return;
            }

            healthAggregator = healthAggregator ?? new DefaultHealthAggregator();
            contributors = contributors ?? new List<IHealthContributor>() { new DiskSpaceContributor(new DiskSpaceContributorOptions(configuration)) };
            var ep = new HealthEndpoint(options, healthAggregator, contributors, CreateLogger<HealthEndpoint>(loggerFactory));
            var handler = new HealthHandler(ep, SecurityServices, _mgmtOptions, CreateLogger<HealthHandler>(loggerFactory));
            ConfiguredHandlers.Add(handler);
        }

        public static void UseInfoActuator(IConfiguration configuration, IEnumerable<IInfoContributor> contributors = null, ILoggerFactory loggerFactory = null)
        {
            var options = new InfoEndpointOptions(configuration);
            _mgmtOptions.RegisterEndpointOptions(configuration, options);

            if (ConfiguredHandlers.OfType<InfoHandler>().Any())
            {
                return;
            }

            contributors = contributors ?? new List<IInfoContributor>() { new GitInfoContributor(), new AppSettingsInfoContributor(configuration) };
            var ep = new InfoEndpoint(options, contributors, CreateLogger<InfoEndpoint>(loggerFactory));
            var handler = new InfoHandler(ep, SecurityServices, _mgmtOptions, CreateLogger<InfoHandler>(loggerFactory));
            ConfiguredHandlers.Add(handler);
        }

        public static void UseLoggerActuator(IConfiguration configuration, ILoggerProvider loggerProvider, ILoggerFactory loggerFactory = null)
        {
            var options = new LoggersEndpointOptions(configuration);
            _mgmtOptions.RegisterEndpointOptions(configuration, options);

            var ep = new LoggersEndpoint(options, loggerProvider as IDynamicLoggerProvider, CreateLogger<LoggersEndpoint>(loggerFactory));
            var handler = new LoggersHandler(ep, SecurityServices, _mgmtOptions, CreateLogger<LoggersHandler>(loggerFactory));
            ConfiguredHandlers.Add(handler);
        }

        public static void UseThreadDumpActuator(IConfiguration configuration, IThreadDumper threadDumper = null, ILoggerFactory loggerFactory = null)
        {
            UseThreadDumpActuator(configuration, MediaTypeVersion.V1, threadDumper, loggerFactory);
        }

        public static void UseThreadDumpActuator(IConfiguration configuration, MediaTypeVersion version, IThreadDumper threadDumper = null, ILoggerFactory loggerFactory = null)
        {
            var options = new ThreadDumpEndpointOptions(configuration);
            if (version == MediaTypeVersion.V2 && options.Id == "dump")
            {
                options.Id = "threaddump";
            }

            _mgmtOptions.RegisterEndpointOptions(configuration, options);
            threadDumper = threadDumper ?? new ThreadDumper(options);
            IActuatorHandler handler;

            switch (version)
            {
                case MediaTypeVersion.V1:
                    var ep = new ThreadDumpEndpoint(options, threadDumper, CreateLogger<ThreadDumpEndpoint>(loggerFactory));
                    handler = new ThreadDumpHandler(ep, SecurityServices, _mgmtOptions, CreateLogger<ThreadDumpHandler>(loggerFactory));
                    break;
                default:
                    var ep2 = new ThreadDumpEndpoint_v2(options, threadDumper, CreateLogger<ThreadDumpEndpoint_v2>(loggerFactory));
                    handler = new ThreadDumpHandler_v2(ep2, SecurityServices, _mgmtOptions, CreateLogger<ThreadDumpHandler_v2>(loggerFactory));
                    break;
            }

            ConfiguredHandlers.Add(handler);
        }

        public static void UseTraceActuator(IConfiguration configuration, ITraceRepository traceRepository = null, ILoggerFactory loggerFactory = null)
        {
            UseTraceActuator(configuration, MediaTypeVersion.V1, traceRepository, loggerFactory);
        }

        public static void UseTraceActuator(IConfiguration configuration, MediaTypeVersion version, ITraceRepository traceRepository = null, ILoggerFactory loggerFactory = null)
        {
            switch (version)
            {
                case MediaTypeVersion.V1:
                    UseTraceActuatorComponents(configuration, traceRepository, loggerFactory);
                    break;
                default:
                    UseHttpTraceActuator(configuration, null, loggerFactory);
                    break;
            }
        }

        public static void UseHttpTraceActuator(IConfiguration configuration, IHttpTraceRepository traceRepository = null, ILoggerFactory loggerFactory = null)
        {
            var options = new HttpTraceEndpointOptions(configuration);
            _mgmtOptions.RegisterEndpointOptions(configuration, options);
            traceRepository = traceRepository ?? new HttpTraceDiagnosticObserver(options, CreateLogger<HttpTraceDiagnosticObserver>(loggerFactory));
            DiagnosticsManager.Instance.Observers.Add((IDiagnosticObserver)traceRepository);
            var ep = new HttpTraceEndpoint(options, traceRepository, CreateLogger<HttpTraceEndpoint>(loggerFactory));
            var handler = new HttpTraceHandler(ep, SecurityServices, _mgmtOptions, CreateLogger<HttpTraceHandler>(loggerFactory));
            ConfiguredHandlers.Add(handler);
        }

        public static void UseRefreshActuator(IConfiguration configuration, ILoggerFactory loggerFactory = null)
        {
            var options = new RefreshEndpointOptions(configuration);
            _mgmtOptions.RegisterEndpointOptions(configuration, options);
            var ep = new RefreshEndpoint(options, configuration, CreateLogger<RefreshEndpoint>(loggerFactory));
            var handler = new RefreshHandler(ep, SecurityServices, _mgmtOptions, CreateLogger<RefreshHandler>(loggerFactory));
            ConfiguredHandlers.Add(handler);
        }

        public static void UseEnvActuator(IConfiguration configuration, IHostingEnvironment hostingEnvironment = null, ILoggerFactory loggerFactory = null)
        {
            var options = new EnvEndpointOptions(configuration);
            _mgmtOptions.RegisterEndpointOptions(configuration, options);
            hostingEnvironment = hostingEnvironment ?? new DefaultHostingEnvironment("development");
            var ep = new EnvEndpoint(options, configuration, hostingEnvironment, CreateLogger<EnvEndpoint>(loggerFactory));
            var handler = new EnvHandler(ep, SecurityServices, _mgmtOptions, CreateLogger<EnvHandler>(loggerFactory));
            ConfiguredHandlers.Add(handler);
        }

        public static void UseMetricsActuator(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            var options = new MetricsEndpointOptions(configuration);
            _mgmtOptions.RegisterEndpointOptions(configuration, options);

            var hostObserver = new AspNetHostingObserver(options, OpenCensusStats.Instance, OpenCensusTags.Instance, CreateLogger<AspNetHostingObserver>(loggerFactory));
            var clrObserver = new CLRRuntimeObserver(options, OpenCensusStats.Instance, OpenCensusTags.Instance, CreateLogger<CLRRuntimeObserver>(loggerFactory));
            DiagnosticsManager.Instance.Observers.Add((IDiagnosticObserver)hostObserver);
            DiagnosticsManager.Instance.Observers.Add((IDiagnosticObserver)clrObserver);

            var clrSource = new CLRRuntimeSource();
            DiagnosticsManager.Instance.Sources.Add(clrSource);
            var ep = new MetricsEndpoint(options, OpenCensusStats.Instance, CreateLogger<MetricsEndpoint>(loggerFactory));
            var handler = new MetricsHandler(ep, SecurityServices, _mgmtOptions, CreateLogger<MetricsHandler>(loggerFactory));
            ConfiguredHandlers.Add(handler);
        }

        public static void UseMappingsActuator(IConfiguration configuration, IApiExplorer apiExplorer = null, ILoggerFactory loggerFactory = null)
        {
            var options = new MappingsEndpointOptions(configuration);
            _mgmtOptions.RegisterEndpointOptions(configuration, options);
            var ep = new MappingsEndpoint(options, CreateLogger<MappingsEndpoint>(loggerFactory));
            var handler = new MappingsHandler(ep, SecurityServices, apiExplorer, _mgmtOptions, CreateLogger<MappingsHandler>(loggerFactory));

            ConfiguredHandlers.Add(handler);
        }

        public static IList<IActuatorHandler> ConfiguredHandlers { get; } = new List<IActuatorHandler>();

        internal static List<ISecurityService> SecurityServices { get; } = new List<ISecurityService>();

        private static ILogger<T> CreateLogger<T>(ILoggerFactory loggerFactory)
        {
            return loggerFactory != null ? loggerFactory.CreateLogger<T>() : LoggerFactory?.CreateLogger<T>();
        }

        private static List<IManagementOptions> _mgmtOptions = new List<IManagementOptions>();

        public static void ClearManagementOptions()
        {
            _mgmtOptions.Clear();
        }

        private static void RegisterEndpointOptions(this IEnumerable<IManagementOptions> mgmtOptions, IConfiguration configuration, IEndpointOptions options)
        {
            if (!mgmtOptions.Any())
            {
                _mgmtOptions.Add(new CloudFoundryManagementOptions(configuration));
                _mgmtOptions.Add(new ActuatorManagementOptions(configuration));
            }

            foreach (var mgmt in mgmtOptions)
            {
                if (!mgmt.EndpointOptions.Contains(options))
                {
                    mgmt.EndpointOptions.Add(options);
                }
            }
        }

        private static void UseTraceActuatorComponents(IConfiguration configuration, ITraceRepository traceRepository, ILoggerFactory loggerFactory)
        {
            var options = new TraceEndpointOptions(configuration);
            _mgmtOptions.RegisterEndpointOptions(configuration, options);
            traceRepository = traceRepository ?? new TraceDiagnosticObserver(options, CreateLogger<TraceDiagnosticObserver>(loggerFactory));
            DiagnosticsManager.Instance.Observers.Add((IDiagnosticObserver)traceRepository);
            var ep = new TraceEndpoint(options, traceRepository, CreateLogger<TraceEndpoint>(loggerFactory));
            var handler = new TraceHandler(ep, SecurityServices, _mgmtOptions, CreateLogger<TraceHandler>(loggerFactory));
            ConfiguredHandlers.Add(handler);
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

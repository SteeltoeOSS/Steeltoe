﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Health.Contributor;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Health.Test
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
            switch (Configuration.GetValue<string>("HealthCheckType"))
            {
                case "down":
                    services.AddHealthActuator(Configuration, new Type[] { typeof(DownContributor) });
                    break;
                case "out":
                    services.AddHealthActuator(Configuration, new Type[] { typeof(OutOfSserviceContributor) });
                    break;
                case "unknown":
                    services.AddHealthActuator(Configuration, new Type[] { typeof(UnknownContributor) });
                    break;
                case "defaultAggregator":
                    services.AddHealthActuator(Configuration, new DefaultHealthAggregator(), new Type[] { typeof(DiskSpaceContributor) });
                    break;
                case "microsoftHealthAggregator":
                    services.AddSingleton<IOptionsMonitor<HealthCheckServiceOptions>>(new TestServiceOptions());
                    services.AddHealthActuator(Configuration, new HealthRegistrationsAggregator(), new Type[] { typeof(DiskSpaceContributor) });
                    break;
                default:
                    services.AddHealthActuator(Configuration);
                    break;
            }
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.Map<HealthEndpointCore>();
            });
        }
    }

#pragma warning disable SA1402 // File may only contain a single type
    public class TestHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => new HealthCheckResult(HealthStatus.Healthy));
        }
    }

    public class TestServiceOptions : IOptionsMonitor<HealthCheckServiceOptions>, IDisposable
#pragma warning restore SA1402 // File may only contain a single type
    {
        private HealthCheckServiceOptions serviceOptions;

        public TestServiceOptions()
        {
            serviceOptions = new HealthCheckServiceOptions();
            serviceOptions.Registrations.Add(new HealthCheckRegistration("test", (provider) => new TestHealthCheck(), HealthStatus.Unhealthy, new string[] { "tags" }.ToList()));
        }

        public HealthCheckServiceOptions CurrentValue => serviceOptions;

        public void Dispose()
        {
        }

        public HealthCheckServiceOptions Get(string name)
        {
            return serviceOptions;
        }

        public IDisposable OnChange(Action<HealthCheckServiceOptions, string> listener)
        {
            return this;
        }
    }
}

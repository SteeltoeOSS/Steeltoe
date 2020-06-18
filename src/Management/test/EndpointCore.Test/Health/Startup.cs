// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Health.Contributor;
using System;

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
}

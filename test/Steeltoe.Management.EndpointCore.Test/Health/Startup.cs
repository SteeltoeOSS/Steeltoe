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

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

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
                default:
                    services.AddHealthActuator(Configuration);
                    break;
            }
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseHealthActuator();
        }
    }
}

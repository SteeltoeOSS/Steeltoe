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

using Microsoft.Extensions.Configuration;
using Owin;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Health.Test;
using Steeltoe.Management.EndpointOwin.Test;
using System.Collections.Generic;

namespace Steeltoe.Management.EndpointOwin.Health.Test
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(OwinTestHelpers.Appsettings);
            var config = builder.Build();

            app.UseHealthActuator(config);
            app.UseHealthActuator(new HealthEndpointOptions { Id = "down" }, new DefaultHealthAggregator(), new List<IHealthContributor> { new DownContributor() });
            app.UseHealthActuator(new HealthEndpointOptions { Id = "out" }, new DefaultHealthAggregator(), new List<IHealthContributor> { new OutOfSserviceContributor() });
            app.UseHealthActuator(new HealthEndpointOptions { Id = "unknown" }, new DefaultHealthAggregator(), new List<IHealthContributor> { new UnknownContributor() });
        }
    }
}

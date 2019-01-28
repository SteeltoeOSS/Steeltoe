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

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Discovery.Eureka;
using Steeltoe.Discovery.Eureka.AppInfo;
using System.Linq;

namespace Steeltoe.Discovery.Client
{
    public class ScopedEurekaHealthCheckHandler : EurekaHealthCheckHandler
    {
        internal IServiceScopeFactory _scopeFactory;

        public ScopedEurekaHealthCheckHandler(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public override InstanceStatus GetStatus(InstanceStatus currentStatus)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                _contributors = scope.ServiceProvider.GetServices<IHealthContributor>().ToList();
                var result = base.GetStatus(currentStatus);
                _contributors = null;
                return result;
            }
        }
    }
}

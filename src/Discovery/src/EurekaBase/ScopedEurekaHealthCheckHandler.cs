// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Discovery.Eureka;
using Steeltoe.Discovery.Eureka.AppInfo;
using System.Linq;

namespace Steeltoe.Discovery.Eureka
{
    public class ScopedEurekaHealthCheckHandler : EurekaHealthCheckHandler
    {
        internal IServiceScopeFactory _scopeFactory;

        public ScopedEurekaHealthCheckHandler(IServiceScopeFactory scopeFactory, ILogger<ScopedEurekaHealthCheckHandler> logger = null)
            : base(logger)
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

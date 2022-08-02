// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Discovery.Eureka.AppInfo;

namespace Steeltoe.Discovery.Eureka;

public class ScopedEurekaHealthCheckHandler : EurekaHealthCheckHandler
{
    internal IServiceScopeFactory ScopeFactory;

    public ScopedEurekaHealthCheckHandler(IServiceScopeFactory scopeFactory, ILogger<ScopedEurekaHealthCheckHandler> logger = null)
        : base(logger)
    {
        ScopeFactory = scopeFactory;
    }

    public override InstanceStatus GetStatus(InstanceStatus currentStatus)
    {
        using IServiceScope scope = ScopeFactory.CreateScope();
        Contributors = scope.ServiceProvider.GetServices<IHealthContributor>().ToList();
        InstanceStatus result = base.GetStatus(currentStatus);
        Contributors = null;
        return result;
    }
}

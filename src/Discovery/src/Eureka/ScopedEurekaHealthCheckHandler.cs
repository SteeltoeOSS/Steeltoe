// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Discovery.Eureka.AppInfo;

namespace Steeltoe.Discovery.Eureka;

public class ScopedEurekaHealthCheckHandler : EurekaHealthCheckHandler
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ScopedEurekaHealthCheckHandler(IServiceScopeFactory scopeFactory, ILogger<ScopedEurekaHealthCheckHandler> logger = null)
        : base(logger)
    {
        ArgumentGuard.NotNull(scopeFactory);

        _scopeFactory = scopeFactory;
    }

    public override async Task<InstanceStatus> GetStatusAsync(InstanceStatus currentStatus, CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();

        try
        {
            Contributors = scope.ServiceProvider.GetServices<IHealthContributor>().ToList();
            InstanceStatus result = await base.GetStatusAsync(currentStatus, cancellationToken);
            return result;
        }
        finally
        {
            Contributors = null;
        }
    }
}

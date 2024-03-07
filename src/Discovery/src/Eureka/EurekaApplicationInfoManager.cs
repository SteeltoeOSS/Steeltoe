// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Configuration;

namespace Steeltoe.Discovery.Eureka;

public sealed class EurekaApplicationInfoManager
{
    private readonly IOptionsMonitor<EurekaInstanceOptions> _instanceOptionsMonitor;
    private readonly ILogger<EurekaApplicationInfoManager> _logger;
    private readonly object _statusChangeLock = new();

    public InstanceInfo InstanceInfo { get; }

    public InstanceStatus InstanceStatus
    {
        get => InstanceInfo.Status ?? InstanceStatus.Unknown;
        set
        {
            lock (_statusChangeLock)
            {
                InstanceStatus previousStatus = InstanceInfo.Status ?? InstanceStatus.Unknown;

                if (previousStatus != value)
                {
                    InstanceInfo.Status = value;

                    try
                    {
                        StatusChanged?.Invoke(this, new InstanceStatusChangedEventArgs(previousStatus, value, InstanceInfo.InstanceId));
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, "StatusChanged event exception");
                    }
                }
            }
        }
    }

    public event EventHandler<InstanceStatusChangedEventArgs>? StatusChanged;

    public EurekaApplicationInfoManager(IOptionsMonitor<EurekaInstanceOptions> instanceOptionsMonitor, ILogger<EurekaApplicationInfoManager> logger)
    {
        ArgumentGuard.NotNull(instanceOptionsMonitor);
        ArgumentGuard.NotNull(logger);

        _instanceOptionsMonitor = instanceOptionsMonitor;
        _logger = logger;
        InstanceInfo = InstanceInfo.FromConfiguration(instanceOptionsMonitor.CurrentValue);
    }

    internal void RefreshLeaseInfo()
    {
        if (InstanceInfo.LeaseInfo == null)
        {
            return;
        }

        EurekaInstanceOptions instanceOptions = _instanceOptionsMonitor.CurrentValue;

        if (InstanceInfo.LeaseInfo.Duration != instanceOptions.LeaseExpirationDuration ||
            InstanceInfo.LeaseInfo.RenewalInterval != instanceOptions.LeaseRenewalInterval)
        {
            // Adapt to changed configuration.
            LeaseInfo newLease = LeaseInfo.FromConfiguration(instanceOptions);

            InstanceInfo.LeaseInfo = newLease;
            InstanceInfo.IsDirty = true;
        }
    }
}

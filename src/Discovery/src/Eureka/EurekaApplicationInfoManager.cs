// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Discovery.Eureka.AppInfo;

namespace Steeltoe.Discovery.Eureka;

public sealed class EurekaApplicationInfoManager
{
    private readonly object _statusChangeLock = new();
    private ILogger<EurekaApplicationInfoManager> _logger;
    private IOptionsMonitor<EurekaInstanceOptions>? _instanceOptionsMonitor;

    public static EurekaApplicationInfoManager SharedInstance { get; } = new();

    public EurekaInstanceOptions? InstanceOptions
    {
        get => _instanceOptionsMonitor?.CurrentValue;
        private set
        {
            if (value != null)
            {
                throw new NotSupportedException();
            }

            _instanceOptionsMonitor = null;
        }
    }

    public InstanceInfo? InstanceInfo { get; internal set; }

    public InstanceStatus InstanceStatus
    {
        get => InstanceInfo?.Status ?? InstanceStatus.Unknown;
        set
        {
            if (InstanceInfo == null)
            {
                return;
            }

            lock (_statusChangeLock)
            {
                InstanceStatus previousInstance = InstanceInfo.Status;

                if (previousInstance != value)
                {
                    InstanceInfo.Status = value;

                    try
                    {
                        StatusChanged?.Invoke(this, new InstanceStatusChangedEventArgs(previousInstance, value, InstanceInfo.InstanceId));
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

    // Constructor used via Dependency Injection
    public EurekaApplicationInfoManager(IOptionsMonitor<EurekaInstanceOptions> instanceOptionsMonitor, ILogger<EurekaApplicationInfoManager> logger)
    {
        ArgumentGuard.NotNull(instanceOptionsMonitor);
        ArgumentGuard.NotNull(logger);

        _instanceOptionsMonitor = instanceOptionsMonitor;
        _logger = logger;
        InstanceInfo = InstanceInfo.FromConfiguration(instanceOptionsMonitor.CurrentValue);
    }

    private EurekaApplicationInfoManager()
    {
        _logger = NullLogger<EurekaApplicationInfoManager>.Instance;
    }

    internal void Initialize(IOptionsMonitor<EurekaInstanceOptions> instanceOptionsMonitor, ILogger<EurekaApplicationInfoManager> logger)
    {
        ArgumentGuard.NotNull(instanceOptionsMonitor);
        ArgumentGuard.NotNull(logger);

        _instanceOptionsMonitor = instanceOptionsMonitor;
        _logger = logger;
        InstanceInfo = InstanceInfo.FromConfiguration(instanceOptionsMonitor.CurrentValue);
    }

    public void RefreshLeaseInfo()
    {
        if (InstanceInfo == null || InstanceOptions == null)
        {
            return;
        }

        if (InstanceInfo.LeaseInfo == null)
        {
            return;
        }

        if (InstanceInfo.LeaseInfo.DurationInSecs != InstanceOptions.LeaseExpirationDurationInSeconds ||
            InstanceInfo.LeaseInfo.RenewalIntervalInSecs != InstanceOptions.LeaseRenewalIntervalInSeconds)
        {
            var newLease = new LeaseInfo(InstanceOptions.LeaseRenewalIntervalInSeconds, InstanceOptions.LeaseExpirationDurationInSeconds);

            InstanceInfo.LeaseInfo = newLease;
            InstanceInfo.IsDirty = true;
        }
    }

    internal static void ResetSharedInstance()
    {
        SharedInstance.InstanceInfo = null;
        SharedInstance.InstanceOptions = null;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Discovery.Eureka.AppInfo;
using System;

namespace Steeltoe.Discovery.Eureka;

public class ApplicationInfoManager
{
    protected ApplicationInfoManager()
    {
    }

    protected static readonly ApplicationInfoManager _instance = new ();
    protected ILogger _logger;

    private readonly object _statusChangedLock = new ();

    public static ApplicationInfoManager Instance => _instance;

    public virtual IEurekaInstanceConfig InstanceConfig { get; protected internal set; }

    public virtual InstanceInfo InstanceInfo { get; protected internal set; }

    public event EventHandler<StatusChangedEventArgs> StatusChanged;

    public virtual InstanceStatus InstanceStatus
    {
        get
        {
            if (InstanceInfo == null)
            {
                return InstanceStatus.UNKNOWN;
            }

            return InstanceInfo.Status;
        }

        set
        {
            if (InstanceInfo == null)
            {
                return;
            }

            lock (_statusChangedLock)
            {
                var prev = InstanceInfo.Status;
                if (prev != value)
                {
                    InstanceInfo.Status = value;

                    try
                    {
                        StatusChanged?.Invoke(this, new StatusChangedEventArgs(prev, value, InstanceInfo.InstanceId));
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError(e, "StatusChanged event exception");
                    }
                }
            }
        }
    }

    public virtual void Initialize(IEurekaInstanceConfig instanceConfig, ILoggerFactory logFactory = null)
    {
        _logger = logFactory?.CreateLogger<ApplicationInfoManager>();
        InstanceConfig = instanceConfig ?? throw new ArgumentNullException(nameof(instanceConfig));
        InstanceInfo = InstanceInfo.FromInstanceConfig(instanceConfig);
    }

    public virtual void RefreshLeaseInfo()
    {
        if (InstanceInfo == null || InstanceConfig == null)
        {
            return;
        }

        if (InstanceInfo.LeaseInfo == null)
        {
            return;
        }

        if (InstanceInfo.LeaseInfo.DurationInSecs != InstanceConfig.LeaseExpirationDurationInSeconds ||
            InstanceInfo.LeaseInfo.RenewalIntervalInSecs != InstanceConfig.LeaseRenewalIntervalInSeconds)
        {
            var newLease = new LeaseInfo
            {
                DurationInSecs = InstanceConfig.LeaseExpirationDurationInSeconds,
                RenewalIntervalInSecs = InstanceConfig.LeaseRenewalIntervalInSeconds
            };
            InstanceInfo.LeaseInfo = newLease;
            InstanceInfo.IsDirty = true;
        }
    }
}

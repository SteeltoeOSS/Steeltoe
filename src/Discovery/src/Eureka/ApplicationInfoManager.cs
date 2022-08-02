// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Discovery.Eureka.AppInfo;

namespace Steeltoe.Discovery.Eureka;

public class ApplicationInfoManager
{
    protected static readonly ApplicationInfoManager InnerInstance = new();

    private readonly object _statusChangedLock = new();
    protected ILogger logger;

    public static ApplicationInfoManager Instance => InnerInstance;

    public virtual IEurekaInstanceConfig InstanceConfig { get; protected internal set; }

    public virtual InstanceInfo InstanceInfo { get; protected internal set; }

    public virtual InstanceStatus InstanceStatus
    {
        get
        {
            if (InstanceInfo == null)
            {
                return InstanceStatus.Unknown;
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
                InstanceStatus prev = InstanceInfo.Status;

                if (prev != value)
                {
                    InstanceInfo.Status = value;

                    try
                    {
                        StatusChanged?.Invoke(this, new StatusChangedEventArgs(prev, value, InstanceInfo.InstanceId));
                    }
                    catch (Exception e)
                    {
                        logger?.LogError(e, "StatusChanged event exception");
                    }
                }
            }
        }
    }

    public event EventHandler<StatusChangedEventArgs> StatusChanged;

    protected ApplicationInfoManager()
    {
    }

    public virtual void Initialize(IEurekaInstanceConfig instanceConfig, ILoggerFactory logFactory = null)
    {
        logger = logFactory?.CreateLogger<ApplicationInfoManager>();
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

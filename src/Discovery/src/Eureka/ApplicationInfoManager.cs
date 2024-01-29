// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Discovery.Eureka.AppInfo;

namespace Steeltoe.Discovery.Eureka;

public class ApplicationInfoManager
{
    protected static readonly ApplicationInfoManager InnerInstance = new();

    private readonly object _statusChangedLock = new();
    protected ILogger logger;

    public static ApplicationInfoManager Instance => InnerInstance;

    public virtual EurekaInstanceOptions InstanceOptions { get; protected internal set; }

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
                    catch (Exception exception)
                    {
                        logger?.LogError(exception, "StatusChanged event exception");
                    }
                }
            }
        }
    }

    public event EventHandler<StatusChangedEventArgs> StatusChanged;

    protected ApplicationInfoManager()
    {
    }

    public virtual void Initialize(EurekaInstanceOptions instanceConfig, ILoggerFactory loggerFactory = null)
    {
        ArgumentGuard.NotNull(instanceConfig);

        logger = loggerFactory?.CreateLogger<ApplicationInfoManager>();
        InstanceOptions = instanceConfig;
        InstanceInfo = InstanceInfo.FromInstanceConfiguration(instanceConfig);
    }

    public virtual void RefreshLeaseInfo()
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
            var newLease = new LeaseInfo
            {
                DurationInSecs = InstanceOptions.LeaseExpirationDurationInSeconds,
                RenewalIntervalInSecs = InstanceOptions.LeaseRenewalIntervalInSeconds
            };

            InstanceInfo.LeaseInfo = newLease;
            InstanceInfo.IsDirty = true;
        }
    }
}

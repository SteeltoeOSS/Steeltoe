// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Eureka.AppInfo;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class ApplicationInfoManagerTest : AbstractBaseTest
{
    private InstanceStatusChangedEventArgs _eventArgs;

    [Fact]
    public void ApplicationInfoManager_IsSingleton()
    {
        Assert.Equal(EurekaApplicationInfoManager.SharedInstance, EurekaApplicationInfoManager.SharedInstance);
    }

    [Fact]
    public void ApplicationInfoManager_Uninitialized()
    {
        Assert.Null(EurekaApplicationInfoManager.SharedInstance.InstanceOptions);
        Assert.Null(EurekaApplicationInfoManager.SharedInstance.InstanceInfo);
        Assert.Equal(InstanceStatus.Unknown, EurekaApplicationInfoManager.SharedInstance.InstanceStatus);
        EurekaApplicationInfoManager.SharedInstance.InstanceStatus = InstanceStatus.Down;
        Assert.Equal(InstanceStatus.Unknown, EurekaApplicationInfoManager.SharedInstance.InstanceStatus);

        // Check no events sent
        EurekaApplicationInfoManager.SharedInstance.StatusChanged += HandleInstanceStatusChanged;
        EurekaApplicationInfoManager.SharedInstance.InstanceStatus = InstanceStatus.Up;
        Assert.Null(_eventArgs);
        EurekaApplicationInfoManager.SharedInstance.StatusChanged -= HandleInstanceStatusChanged;
    }

    [Fact]
    public void Initialize_Initializes_InstanceInfo()
    {
        var instanceOptions = new EurekaInstanceOptions();
        var instanceOptionsMonitor = new TestOptionsMonitor<EurekaInstanceOptions>(instanceOptions);
        EurekaApplicationInfoManager.SharedInstance.Initialize(instanceOptionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);

        Assert.NotNull(EurekaApplicationInfoManager.SharedInstance.InstanceOptions);
        Assert.Equal(instanceOptions, EurekaApplicationInfoManager.SharedInstance.InstanceOptions);
        Assert.NotNull(EurekaApplicationInfoManager.SharedInstance.InstanceInfo);
    }

    [Fact]
    public void StatusChanged_ChangesStatus()
    {
        var instanceOptions = new EurekaInstanceOptions
        {
            IsInstanceEnabledOnInit = false
        };

        var instanceOptionsMonitor = new TestOptionsMonitor<EurekaInstanceOptions>(instanceOptions);
        EurekaApplicationInfoManager.SharedInstance.Initialize(instanceOptionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);

        Assert.Equal(InstanceStatus.Starting, EurekaApplicationInfoManager.SharedInstance.InstanceStatus);
        EurekaApplicationInfoManager.SharedInstance.InstanceStatus = InstanceStatus.Up;
    }

    [Fact]
    public void StatusChanged_ChangesStatus_SendsEvents()
    {
        var instanceOptions = new EurekaInstanceOptions
        {
            IsInstanceEnabledOnInit = false
        };

        var instanceOptionsMonitor = new TestOptionsMonitor<EurekaInstanceOptions>(instanceOptions);
        EurekaApplicationInfoManager.SharedInstance.Initialize(instanceOptionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);
        Assert.Equal(InstanceStatus.Starting, EurekaApplicationInfoManager.SharedInstance.InstanceStatus);

        // Check event sent
        EurekaApplicationInfoManager.SharedInstance.StatusChanged += HandleInstanceStatusChanged;
        EurekaApplicationInfoManager.SharedInstance.InstanceStatus = InstanceStatus.Up;
        Assert.NotNull(_eventArgs);
        Assert.Equal(InstanceStatus.Starting, _eventArgs.Previous);
        Assert.Equal(InstanceStatus.Up, _eventArgs.Current);
        Assert.Equal(EurekaApplicationInfoManager.SharedInstance.InstanceInfo.InstanceId, _eventArgs.InstanceId);
        EurekaApplicationInfoManager.SharedInstance.StatusChanged -= HandleInstanceStatusChanged;
    }

    [Fact]
    public void StatusChanged_RemovesEventHandler()
    {
        var instanceOptions = new EurekaInstanceOptions
        {
            IsInstanceEnabledOnInit = false
        };

        var instanceOptionsMonitor = new TestOptionsMonitor<EurekaInstanceOptions>(instanceOptions);
        EurekaApplicationInfoManager.SharedInstance.Initialize(instanceOptionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);
        Assert.Equal(InstanceStatus.Starting, EurekaApplicationInfoManager.SharedInstance.InstanceStatus);

        // Check event sent
        EurekaApplicationInfoManager.SharedInstance.StatusChanged += HandleInstanceStatusChanged;
        EurekaApplicationInfoManager.SharedInstance.InstanceStatus = InstanceStatus.Up;
        Assert.NotNull(_eventArgs);
        Assert.Equal(InstanceStatus.Starting, _eventArgs.Previous);
        Assert.Equal(InstanceStatus.Up, _eventArgs.Current);
        Assert.Equal(EurekaApplicationInfoManager.SharedInstance.InstanceInfo.InstanceId, _eventArgs.InstanceId);
        _eventArgs = null;
        EurekaApplicationInfoManager.SharedInstance.StatusChanged -= HandleInstanceStatusChanged;
        EurekaApplicationInfoManager.SharedInstance.InstanceStatus = InstanceStatus.Down;
        Assert.Null(_eventArgs);
    }

    [Fact]
    public void RefreshLeaseInfo_UpdatesLeaseInfo()
    {
        var instanceOptions = new EurekaInstanceOptions();
        var instanceOptionsMonitor = new TestOptionsMonitor<EurekaInstanceOptions>(instanceOptions);
        EurekaApplicationInfoManager.SharedInstance.Initialize(instanceOptionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);

        EurekaApplicationInfoManager.SharedInstance.RefreshLeaseInfo();
        InstanceInfo info = EurekaApplicationInfoManager.SharedInstance.InstanceInfo;

        Assert.False(info.IsDirty);
        Assert.Equal(instanceOptions.LeaseExpirationDurationInSeconds, info.LeaseInfo.DurationInSecs);
        Assert.Equal(instanceOptions.LeaseRenewalIntervalInSeconds, info.LeaseInfo.RenewalIntervalInSecs);

        instanceOptions.LeaseRenewalIntervalInSeconds += 100;
        instanceOptions.LeaseExpirationDurationInSeconds += 100;
        EurekaApplicationInfoManager.SharedInstance.RefreshLeaseInfo();
        Assert.True(info.IsDirty);
        Assert.Equal(instanceOptions.LeaseExpirationDurationInSeconds, info.LeaseInfo.DurationInSecs);
        Assert.Equal(instanceOptions.LeaseRenewalIntervalInSeconds, info.LeaseInfo.RenewalIntervalInSecs);
    }

    private void HandleInstanceStatusChanged(object sender, InstanceStatusChangedEventArgs args)
    {
        _eventArgs = args;
    }
}

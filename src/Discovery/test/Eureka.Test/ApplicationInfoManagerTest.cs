// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Configuration;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class ApplicationInfoManagerTest : AbstractBaseTest
{
    private InstanceStatusChangedEventArgs _eventArgs;

    [Fact]
    public void StatusChanged_ChangesStatus()
    {
        var instanceOptions = new EurekaInstanceOptions
        {
            IsInstanceEnabledOnInit = false
        };

        TestOptionsMonitor<EurekaInstanceOptions> optionsMonitor = TestOptionsMonitor.Create(instanceOptions);
        var appManager = new EurekaApplicationInfoManager(optionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);

        Assert.Equal(InstanceStatus.Starting, appManager.InstanceStatus);
        appManager.InstanceStatus = InstanceStatus.Up;
    }

    [Fact]
    public void StatusChanged_ChangesStatus_SendsEvents()
    {
        var instanceOptions = new EurekaInstanceOptions
        {
            IsInstanceEnabledOnInit = false
        };

        TestOptionsMonitor<EurekaInstanceOptions> optionsMonitor = TestOptionsMonitor.Create(instanceOptions);
        var appManager = new EurekaApplicationInfoManager(optionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);

        Assert.Equal(InstanceStatus.Starting, appManager.InstanceStatus);

        // Check event sent
        appManager.StatusChanged += HandleInstanceStatusChanged;
        appManager.InstanceStatus = InstanceStatus.Up;
        Assert.NotNull(_eventArgs);
        Assert.Equal(InstanceStatus.Starting, _eventArgs.Previous);
        Assert.Equal(InstanceStatus.Up, _eventArgs.Current);
        Assert.Equal(appManager.InstanceInfo.InstanceId, _eventArgs.InstanceId);
        appManager.StatusChanged -= HandleInstanceStatusChanged;
    }

    [Fact]
    public void StatusChanged_RemovesEventHandler()
    {
        var instanceOptions = new EurekaInstanceOptions
        {
            IsInstanceEnabledOnInit = false
        };

        TestOptionsMonitor<EurekaInstanceOptions> optionsMonitor = TestOptionsMonitor.Create(instanceOptions);
        var appManager = new EurekaApplicationInfoManager(optionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);

        Assert.Equal(InstanceStatus.Starting, appManager.InstanceStatus);

        // Check event sent
        appManager.StatusChanged += HandleInstanceStatusChanged;
        appManager.InstanceStatus = InstanceStatus.Up;
        Assert.NotNull(_eventArgs);
        Assert.Equal(InstanceStatus.Starting, _eventArgs.Previous);
        Assert.Equal(InstanceStatus.Up, _eventArgs.Current);
        Assert.Equal(appManager.InstanceInfo.InstanceId, _eventArgs.InstanceId);
        _eventArgs = null;
        appManager.StatusChanged -= HandleInstanceStatusChanged;
        appManager.InstanceStatus = InstanceStatus.Down;
        Assert.Null(_eventArgs);
    }

    [Fact]
    public void RefreshLeaseInfo_UpdatesLeaseInfo()
    {
        var instanceOptions = new EurekaInstanceOptions();
        TestOptionsMonitor<EurekaInstanceOptions> optionsMonitor = TestOptionsMonitor.Create(instanceOptions);
        var appManager = new EurekaApplicationInfoManager(optionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);

        appManager.RefreshLeaseInfo();
        InstanceInfo instance = appManager.InstanceInfo;

        Assert.False(instance.IsDirty);
        Assert.Equal(instanceOptions.LeaseExpirationDurationInSeconds, instance.LeaseInfo.Duration.TotalSeconds);
        Assert.Equal(instanceOptions.LeaseRenewalIntervalInSeconds, instance.LeaseInfo.RenewalInterval.TotalSeconds);

        instanceOptions.LeaseRenewalIntervalInSeconds += 100;
        instanceOptions.LeaseExpirationDurationInSeconds += 100;
        appManager.RefreshLeaseInfo();
        Assert.True(instance.IsDirty);
        Assert.Equal(instanceOptions.LeaseExpirationDurationInSeconds, instance.LeaseInfo.Duration.TotalSeconds);
        Assert.Equal(instanceOptions.LeaseRenewalIntervalInSeconds, instance.LeaseInfo.RenewalInterval.TotalSeconds);
    }

    private void HandleInstanceStatusChanged(object sender, InstanceStatusChangedEventArgs args)
    {
        _eventArgs = args;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.AppInfo;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public class ApplicationInfoManagerTest : AbstractBaseTest
{
    private StatusChangedEventArgs _eventArgs;

    [Fact]
    public void ApplicationInfoManager_IsSingleton()
    {
        Assert.Equal(ApplicationInfoManager.Instance, ApplicationInfoManager.Instance);
    }

    [Fact]
    public void ApplicationInfoManager_Uninitialized()
    {
        Assert.Null(ApplicationInfoManager.Instance.InstanceConfig);
        Assert.Null(ApplicationInfoManager.Instance.InstanceInfo);
        Assert.Equal(InstanceStatus.Unknown, ApplicationInfoManager.Instance.InstanceStatus);
        ApplicationInfoManager.Instance.InstanceStatus = InstanceStatus.Down;
        Assert.Equal(InstanceStatus.Unknown, ApplicationInfoManager.Instance.InstanceStatus);

        // Check no events sent
        ApplicationInfoManager.Instance.StatusChanged += HandleInstanceStatusChanged;
        ApplicationInfoManager.Instance.InstanceStatus = InstanceStatus.Up;
        Assert.Null(_eventArgs);
        ApplicationInfoManager.Instance.StatusChanged -= HandleInstanceStatusChanged;
    }

    [Fact]
    public void Initialize_Throws_IfInstanceConfigNull()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => ApplicationInfoManager.Instance.Initialize(null));
        Assert.Contains("instanceConfig", ex.Message);
    }

    [Fact]
    public void Initialize_Initializes_InstanceInfo()
    {
        var configuration = new EurekaInstanceConfiguration();
        ApplicationInfoManager.Instance.Initialize(configuration);

        Assert.NotNull(ApplicationInfoManager.Instance.InstanceConfig);
        Assert.Equal(configuration, ApplicationInfoManager.Instance.InstanceConfig);
        Assert.NotNull(ApplicationInfoManager.Instance.InstanceInfo);
    }

    [Fact]
    public void StatusChanged_ChangesStatus()
    {
        var configuration = new EurekaInstanceConfiguration();
        ApplicationInfoManager.Instance.Initialize(configuration);

        Assert.Equal(InstanceStatus.Starting, ApplicationInfoManager.Instance.InstanceStatus);
        ApplicationInfoManager.Instance.InstanceStatus = InstanceStatus.Up;
    }

    [Fact]
    public void StatusChanged_ChangesStatus_SendsEvents()
    {
        var configuration = new EurekaInstanceConfiguration();
        ApplicationInfoManager.Instance.Initialize(configuration);
        Assert.Equal(InstanceStatus.Starting, ApplicationInfoManager.Instance.InstanceStatus);

        // Check event sent
        ApplicationInfoManager.Instance.StatusChanged += HandleInstanceStatusChanged;
        ApplicationInfoManager.Instance.InstanceStatus = InstanceStatus.Up;
        Assert.NotNull(_eventArgs);
        Assert.Equal(InstanceStatus.Starting, _eventArgs.Previous);
        Assert.Equal(InstanceStatus.Up, _eventArgs.Current);
        Assert.Equal(ApplicationInfoManager.Instance.InstanceInfo.InstanceId, _eventArgs.InstanceId);
        ApplicationInfoManager.Instance.StatusChanged -= HandleInstanceStatusChanged;
    }

    [Fact]
    public void StatusChanged_RemovesEventHandler()
    {
        var configuration = new EurekaInstanceConfiguration();
        ApplicationInfoManager.Instance.Initialize(configuration);
        Assert.Equal(InstanceStatus.Starting, ApplicationInfoManager.Instance.InstanceStatus);

        // Check event sent
        ApplicationInfoManager.Instance.StatusChanged += HandleInstanceStatusChanged;
        ApplicationInfoManager.Instance.InstanceStatus = InstanceStatus.Up;
        Assert.NotNull(_eventArgs);
        Assert.Equal(InstanceStatus.Starting, _eventArgs.Previous);
        Assert.Equal(InstanceStatus.Up, _eventArgs.Current);
        Assert.Equal(ApplicationInfoManager.Instance.InstanceInfo.InstanceId, _eventArgs.InstanceId);
        _eventArgs = null;
        ApplicationInfoManager.Instance.StatusChanged -= HandleInstanceStatusChanged;
        ApplicationInfoManager.Instance.InstanceStatus = InstanceStatus.Down;
        Assert.Null(_eventArgs);
    }

    [Fact]
    public void RefreshLeaseInfo_UpdatesLeaseInfo()
    {
        var configuration = new EurekaInstanceConfiguration();
        ApplicationInfoManager.Instance.Initialize(configuration);

        ApplicationInfoManager.Instance.RefreshLeaseInfo();
        InstanceInfo info = ApplicationInfoManager.Instance.InstanceInfo;

        Assert.False(info.IsDirty);
        Assert.Equal(configuration.LeaseExpirationDurationInSeconds, info.LeaseInfo.DurationInSecs);
        Assert.Equal(configuration.LeaseRenewalIntervalInSeconds, info.LeaseInfo.RenewalIntervalInSecs);

        configuration.LeaseRenewalIntervalInSeconds += 100;
        configuration.LeaseExpirationDurationInSeconds += 100;
        ApplicationInfoManager.Instance.RefreshLeaseInfo();
        Assert.True(info.IsDirty);
        Assert.Equal(configuration.LeaseExpirationDurationInSeconds, info.LeaseInfo.DurationInSecs);
        Assert.Equal(configuration.LeaseRenewalIntervalInSeconds, info.LeaseInfo.RenewalIntervalInSecs);
    }

    private void HandleInstanceStatusChanged(object sender, StatusChangedEventArgs args)
    {
        _eventArgs = args;
    }
}

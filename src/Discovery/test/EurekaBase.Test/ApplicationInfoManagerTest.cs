// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.AppInfo;
using System;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test
{
    public class ApplicationInfoManagerTest : AbstractBaseTest
    {
        private StatusChangedArgs eventArg = null;

        public ApplicationInfoManagerTest()
            : base()
        {
            eventArg = null;
        }

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
            Assert.Equal(InstanceStatus.UNKNOWN, ApplicationInfoManager.Instance.InstanceStatus);
            ApplicationInfoManager.Instance.InstanceStatus = InstanceStatus.DOWN;
            Assert.Equal(InstanceStatus.UNKNOWN, ApplicationInfoManager.Instance.InstanceStatus);

            // Check no events sent
            ApplicationInfoManager.Instance.StatusChangedEvent += Instance_StatusChangedEvent;
            ApplicationInfoManager.Instance.InstanceStatus = InstanceStatus.UP;
            Assert.Null(eventArg);
            ApplicationInfoManager.Instance.StatusChangedEvent -= Instance_StatusChangedEvent;
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
            var config = new EurekaInstanceConfig();
            ApplicationInfoManager.Instance.Initialize(config);

            Assert.NotNull(ApplicationInfoManager.Instance.InstanceConfig);
            Assert.Equal(config, ApplicationInfoManager.Instance.InstanceConfig);
            Assert.NotNull(ApplicationInfoManager.Instance.InstanceInfo);
        }

        [Fact]
        public void StatusChanged_ChangesStatus()
        {
            var config = new EurekaInstanceConfig();
            ApplicationInfoManager.Instance.Initialize(config);

            Assert.Equal(InstanceStatus.STARTING, ApplicationInfoManager.Instance.InstanceStatus);
            ApplicationInfoManager.Instance.InstanceStatus = InstanceStatus.UP;
        }

        [Fact]
        public void StatusChanged_ChangesStatus_SendsEvents()
        {
            var config = new EurekaInstanceConfig();
            ApplicationInfoManager.Instance.Initialize(config);
            Assert.Equal(InstanceStatus.STARTING, ApplicationInfoManager.Instance.InstanceStatus);

            // Check event sent
            ApplicationInfoManager.Instance.StatusChangedEvent += Instance_StatusChangedEvent;
            ApplicationInfoManager.Instance.InstanceStatus = InstanceStatus.UP;
            Assert.NotNull(eventArg);
            Assert.Equal(InstanceStatus.STARTING, eventArg.Previous);
            Assert.Equal(InstanceStatus.UP, eventArg.Current);
            Assert.Equal(ApplicationInfoManager.Instance.InstanceInfo.InstanceId, eventArg.InstanceId);
            ApplicationInfoManager.Instance.StatusChangedEvent -= Instance_StatusChangedEvent;
        }

        [Fact]
        public void StatusChanged_RemovesEventHandler()
        {
            var config = new EurekaInstanceConfig();
            ApplicationInfoManager.Instance.Initialize(config);
            Assert.Equal(InstanceStatus.STARTING, ApplicationInfoManager.Instance.InstanceStatus);

            // Check event sent
            ApplicationInfoManager.Instance.StatusChangedEvent += Instance_StatusChangedEvent;
            ApplicationInfoManager.Instance.InstanceStatus = InstanceStatus.UP;
            Assert.NotNull(eventArg);
            Assert.Equal(InstanceStatus.STARTING, eventArg.Previous);
            Assert.Equal(InstanceStatus.UP, eventArg.Current);
            Assert.Equal(ApplicationInfoManager.Instance.InstanceInfo.InstanceId, eventArg.InstanceId);
            eventArg = null;
            ApplicationInfoManager.Instance.StatusChangedEvent -= Instance_StatusChangedEvent;
            ApplicationInfoManager.Instance.InstanceStatus = InstanceStatus.DOWN;
            Assert.Null(eventArg);
        }

        [Fact]
        public void RefreshLeaseInfo_UpdatesLeaseInfo()
        {
            var config = new EurekaInstanceConfig();
            ApplicationInfoManager.Instance.Initialize(config);

            ApplicationInfoManager.Instance.RefreshLeaseInfo();
            var info = ApplicationInfoManager.Instance.InstanceInfo;

            Assert.False(info.IsDirty);
            Assert.Equal(config.LeaseExpirationDurationInSeconds, info.LeaseInfo.DurationInSecs);
            Assert.Equal(config.LeaseRenewalIntervalInSeconds, info.LeaseInfo.RenewalIntervalInSecs);

            config.LeaseRenewalIntervalInSeconds += 100;
            config.LeaseExpirationDurationInSeconds += 100;
            ApplicationInfoManager.Instance.RefreshLeaseInfo();
            Assert.True(info.IsDirty);
            Assert.Equal(config.LeaseExpirationDurationInSeconds, info.LeaseInfo.DurationInSecs);
            Assert.Equal(config.LeaseRenewalIntervalInSeconds, info.LeaseInfo.RenewalIntervalInSecs);
        }

        private void Instance_StatusChangedEvent(object sender, StatusChangedArgs args)
        {
            eventArg = args;
        }
    }
}

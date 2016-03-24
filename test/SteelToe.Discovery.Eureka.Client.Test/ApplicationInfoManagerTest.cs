//
// Copyright 2015 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using SteelToe.Discovery.Eureka.AppInfo;
using SteelToe.Discovery.Eureka.Client.Test;
using System;
using Xunit;

namespace SteelToe.Discovery.Eureka.Test
{
    public class ApplicationInfoManagerTest : AbstractBaseTest
    {
        private StatusChangedArgs eventArg = null;
        public ApplicationInfoManagerTest() : base()
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
            EurekaInstanceConfig config = new EurekaInstanceConfig();
            ApplicationInfoManager.Instance.Initialize(config);

            Assert.NotNull(ApplicationInfoManager.Instance.InstanceConfig);
            Assert.Equal(config, ApplicationInfoManager.Instance.InstanceConfig);
            Assert.NotNull(ApplicationInfoManager.Instance.InstanceInfo);

        }

        [Fact]
        public void StatusChanged_ChangesStatus()
        {
            EurekaInstanceConfig config = new EurekaInstanceConfig();
            ApplicationInfoManager.Instance.Initialize(config);

            Assert.Equal(InstanceStatus.STARTING, ApplicationInfoManager.Instance.InstanceStatus);
            ApplicationInfoManager.Instance.InstanceStatus = InstanceStatus.UP;
        }

        [Fact]
        public void StatusChanged_ChangesStatus_SendsEvents()
        {
            EurekaInstanceConfig config = new EurekaInstanceConfig();
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
            EurekaInstanceConfig config = new EurekaInstanceConfig();
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
            EurekaInstanceConfig config = new EurekaInstanceConfig();
            ApplicationInfoManager.Instance.Initialize(config);

            ApplicationInfoManager.Instance.RefreshLeaseInfo();
            InstanceInfo info = ApplicationInfoManager.Instance.InstanceInfo;

            Assert.False(info.IsDirty);
            Assert.Equal(config.LeaseExpirationDurationInSeconds, info.LeaseInfo.DurationInSecs);
            Assert.Equal(config.LeaseRenewalIntervalInSeconds, info.LeaseInfo.RenewalIntervalInSecs);

            config.LeaseRenewalIntervalInSeconds = config.LeaseRenewalIntervalInSeconds + 100;
            config.LeaseExpirationDurationInSeconds = config.LeaseExpirationDurationInSeconds + 100;
            ApplicationInfoManager.Instance.RefreshLeaseInfo();
            Assert.True(info.IsDirty);
            Assert.Equal(config.LeaseExpirationDurationInSeconds, info.LeaseInfo.DurationInSecs);
            Assert.Equal(config.LeaseRenewalIntervalInSeconds, info.LeaseInfo.RenewalIntervalInSecs);

        }

        private void Instance_StatusChangedEvent(object sender, StatusChangedArgs args)
        {
            this.eventArg = args;
        }
    }
}


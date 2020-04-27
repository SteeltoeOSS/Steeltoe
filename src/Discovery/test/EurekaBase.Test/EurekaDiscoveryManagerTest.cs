// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Xunit;

namespace Steeltoe.Discovery.Eureka.Test
{
    public class EurekaDiscoveryManagerTest : AbstractBaseTest
    {
        [Fact]
        public void Constructor_Initializes_Correctly()
        {
            var instOptions = new EurekaInstanceOptions();
            var clientOptions = new EurekaClientOptions() { EurekaServerRetryCount = 0 };
            var wrapInst = new TestOptionMonitorWrapper<EurekaInstanceOptions>(instOptions);
            var wrapClient = new TestOptionMonitorWrapper<EurekaClientOptions>(clientOptions);
            var appMgr = new EurekaApplicationInfoManager(wrapInst);
            var client = new EurekaDiscoveryClient(wrapClient, wrapInst, appMgr);

            var mgr = new EurekaDiscoveryManager(wrapClient, wrapInst, client);
            Assert.Equal(instOptions, mgr.InstanceConfig);
            Assert.Equal(clientOptions, mgr.ClientConfig);
            Assert.Equal(client, mgr.Client);
        }
    }
}

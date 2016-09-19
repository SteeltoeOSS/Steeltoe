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

using Xunit;

namespace Steeltoe.Discovery.Client.Test
{
    public class EurekaClientOptionsTest : AbstractBaseTest
    {

        [Fact]
        public void Constructor_Intializes_Defaults()
        {
            EurekaClientOptions opts = new EurekaClientOptions();
            Assert.True(opts.Enabled);
            Assert.Equal(EurekaClientOptions.Default_RegistryFetchIntervalSeconds, opts.RegistryFetchIntervalSeconds);
            Assert.Equal(EurekaClientOptions.Default_InstanceInfoReplicationIntervalSeconds, opts.InstanceInfoReplicationIntervalSeconds);
            Assert.Null(opts.ProxyHost);
            Assert.Equal(0, opts.ProxyPort);
            Assert.Null(opts.ProxyUserName);
            Assert.Null(opts.ProxyPassword);
            Assert.True(opts.ShouldGZipContent);
            Assert.Equal(EurekaClientOptions.Default_EurekaServerConnectTimeoutSeconds, opts.EurekaServerConnectTimeoutSeconds);
            Assert.True(opts.ShouldRegisterWithEureka);
            Assert.False(opts.AllowRedirects);
            Assert.False(opts.ShouldDisableDelta);
            Assert.True(opts.ShouldFilterOnlyUpInstances);
            Assert.True(opts.ShouldFetchRegistry);
            Assert.Null(opts.RegistryRefreshSingleVipAddress);
            Assert.True(opts.ShouldOnDemandUpdateStatusChange);
            Assert.Equal(EurekaClientOptions.Default_ServerServiceUrl, opts.EurekaServerServiceUrls);
        }
    }
}

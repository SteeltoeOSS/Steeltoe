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
    public class EurekaClientConfigTest : AbstractBaseTest
    {
        [Fact]
        public void DefaultConstructor_InitializedWithDefaults()
        {
            var config = new EurekaClientConfig();
            Assert.Equal(EurekaClientConfig.Default_RegistryFetchIntervalSeconds, config.RegistryFetchIntervalSeconds);
            Assert.True(config.ShouldGZipContent);
            Assert.Equal(EurekaClientConfig.Default_EurekaServerConnectTimeoutSeconds, config.EurekaServerConnectTimeoutSeconds);
            Assert.True(config.ShouldRegisterWithEureka);
            Assert.False(config.ShouldDisableDelta);
            Assert.True(config.ShouldFilterOnlyUpInstances);
            Assert.True(config.ShouldFetchRegistry);
            Assert.True(config.ShouldOnDemandUpdateStatusChange);
        }
    }
}

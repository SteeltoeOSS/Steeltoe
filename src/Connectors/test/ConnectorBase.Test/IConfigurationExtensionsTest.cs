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

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.Redis.Test;
using Steeltoe.Connector.Services;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Linq;
using Xunit;

namespace Steeltoe.Connector.Test
{
    public class IConfigurationExtensionsTest
    {
        [Fact]
        public void GetServiceInfos_GetsCFRedisServiceInfos()
        {
            // arrange
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", RedisCacheTestHelpers.SingleServerVCAP);

            var config = new ConfigurationBuilder().AddCloudFoundry().Build();

            // act
            var infos = config.GetServiceInfos(typeof(RedisServiceInfo));

            // assert
            Assert.NotEmpty(infos);
        }

        [Fact]
        public void GetServiceInfos_GetsRedisServiceInfos()
        {
            // arrange
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", string.Empty);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", string.Empty);
            var config = new ConfigurationBuilder().AddInMemoryCollection(RedisCacheTestHelpers.SingleServerAsDictionary).Build();

            // act
            var infos = config.GetServiceInfos(typeof(RedisServiceInfo));

            // assert
            Assert.NotEmpty(infos);
            var si = infos.First() as RedisServiceInfo;
            Assert.Equal(RedisCacheTestHelpers.SingleServerAsDictionary["services:p-redis:0:credentials:host"], si.Host);
            Assert.Equal(RedisCacheTestHelpers.SingleServerAsDictionary["services:p-redis:0:credentials:password"], si.Password);
            Assert.Equal(RedisCacheTestHelpers.SingleServerAsDictionary["services:p-redis:0:credentials:port"], si.Port.ToString());
        }
    }
}

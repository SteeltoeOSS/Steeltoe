// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

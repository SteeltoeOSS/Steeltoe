// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Apache.Geode.Client;
using Steeltoe.CloudFoundry.Connector.App;
using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.CloudFoundry.Connector.Test;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.GemFire.Test
{
    public class GemFireConnectorFactoryTest
    {
        [Fact]
        public void CanReturnObjects()
        {
            // arrange
            var config = new GemFireConnectorOptions()
            {
                Locators = { "10.194.45.168[55221]" },
                Password = "password",
                Username = "username"
            };
            var si = new GemFireServiceInfo("myId")
            {
                ApplicationInfo = new ApplicationInstanceInfo()
                {
                    InstanceId = "instanceId"
                }
            };

            // act
            var factory = new GemFireConnectorFactory(config, si);
            var cacheFactory = factory.CreateCacheFactory(null, typeof(BasicAuthInitialize));
            Assert.IsType<CacheFactory>(cacheFactory);
            var cache = factory.CreateCache(cacheFactory);
            Assert.IsType<Cache>(cache);
            var pool = factory.CreatePoolFactory(cache);
            Assert.IsType<PoolFactory>(pool);
        }
    }
}

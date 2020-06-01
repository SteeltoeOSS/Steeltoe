// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.CloudFoundry.Connector.GemFire.Test
{
    public class GemFireTypeLocatorTest
    {
        [Fact]
        public void Property_Can_Locate_ConnectionTypes()
        {
            // arrange -- handled by including a compatible GemFire assembly

            // act
            var regionFactory = GemFireTypeLocator.RegionFactory;
            var poolFactory = GemFireTypeLocator.PoolFactory;
            var cacheFactory = GemFireTypeLocator.CacheFactory;
            var cache = GemFireTypeLocator.Cache;

            // assert
            Assert.NotNull(regionFactory);
            Assert.NotNull(poolFactory);
            Assert.NotNull(cacheFactory);
            Assert.NotNull(cache);
        }

        [Fact]
        public void Throws_When_ConnectionType_NotFound()
        {
            // arrange
            var msftAssemblies = GemFireTypeLocator.Assemblies;
            GemFireTypeLocator.Assemblies = new string[] { "something-Wrong" };

            // act
            var exception = Assert.Throws<ConnectorException>(() => GemFireTypeLocator.CacheFactory);

            // assert
            Assert.Equal($"Unable to find CacheFactory, are you missing the Pivotal GemFire dll?", exception.Message);

            // reset
            GemFireTypeLocator.Assemblies = msftAssemblies;
        }
    }
}

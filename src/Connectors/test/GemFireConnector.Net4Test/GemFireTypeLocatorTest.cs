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

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

namespace Steeltoe.CloudFoundry.Connector.Redis.Test
{
    public class RedisTypeLocatorTest
    {
        [Fact]
        public void Property_Can_Locate_ConnectionTypes()
        {
            // arrange -- handled by including a compatible Redis NuGet package

            // act
            var msftInterface = RedisTypeLocator.MicrosoftInterface;
            var msftImplement = RedisTypeLocator.MicrosoftImplementation;
            var msftOptions = RedisTypeLocator.MicrosoftOptions;
            var stackInterface = RedisTypeLocator.StackExchangeInterface;
            var stackImplement = RedisTypeLocator.StackExchangeImplementation;
            var stackOptions = RedisTypeLocator.StackExchangeOptions;

            // assert
            Assert.NotNull(msftInterface);
            Assert.NotNull(msftImplement);
            Assert.NotNull(msftOptions);
            Assert.NotNull(stackInterface);
            Assert.NotNull(stackImplement);
            Assert.NotNull(stackOptions);
        }

        [Fact]
        public void Throws_When_ConnectionType_NotFound()
        {
            // arrange
            var msftAssemblies = RedisTypeLocator.MicrosoftAssemblies;
            var stackAssemblies = RedisTypeLocator.StackExchangeAssemblies;
            RedisTypeLocator.MicrosoftAssemblies = new string[] { "something-Wrong" };
            RedisTypeLocator.StackExchangeAssemblies = RedisTypeLocator.MicrosoftAssemblies;

            // act
            var msftException = Assert.Throws<ConnectorException>(() => RedisTypeLocator.MicrosoftInterface);
            var stackException = Assert.Throws<ConnectorException>(() => RedisTypeLocator.StackExchangeInterface);

            // assert
            Assert.Equal($"Unable to find {RedisTypeLocator.MicrosoftInterfaceTypeNames[0]}, are you missing a Microsoft Caching NuGet Reference?", msftException.Message);
            Assert.Equal($"Unable to find {RedisTypeLocator.StackExchangeInterfaceTypeNames[0]}, are you missing a Stack Exchange Redis NuGet Reference?", stackException.Message);

            // reset
            RedisTypeLocator.MicrosoftAssemblies = msftAssemblies;
            RedisTypeLocator.StackExchangeAssemblies = stackAssemblies;
        }
    }
}

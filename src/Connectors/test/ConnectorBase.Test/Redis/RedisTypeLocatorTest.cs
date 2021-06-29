// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Connector.Redis.Test
{
    [Collection("Redis")]
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

        [Fact(Skip = "Changing the expected assemblies breaks other tests when collections don't work as expected")]
        public void Throws_When_ConnectionType_NotFound()
        {
            // arrange
            var msftAssemblies = RedisTypeLocator.MicrosoftAssemblies;
            var stackAssemblies = RedisTypeLocator.StackExchangeAssemblies;
            RedisTypeLocator.MicrosoftAssemblies = new string[] { "something-Wrong" };
            RedisTypeLocator.StackExchangeAssemblies = new string[] { "something-Wrong" };

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

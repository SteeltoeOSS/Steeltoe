// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Connector.Redis.Test;

[Collection("Redis")]
public class RedisTypeLocatorTest
{
    [Fact]
    public void Property_Can_Locate_ConnectionTypes()
    {
        // arrange -- handled by including a compatible Redis NuGet package
        var microsoftInterface = RedisTypeLocator.MicrosoftInterface;
        var microsoftImplementation = RedisTypeLocator.MicrosoftImplementation;
        var microsoftOptions = RedisTypeLocator.MicrosoftOptions;
        var stackInterface = RedisTypeLocator.StackExchangeInterface;
        var stackImplement = RedisTypeLocator.StackExchangeImplementation;
        var stackOptions = RedisTypeLocator.StackExchangeOptions;

        Assert.NotNull(microsoftInterface);
        Assert.NotNull(microsoftImplementation);
        Assert.NotNull(microsoftOptions);
        Assert.NotNull(stackInterface);
        Assert.NotNull(stackImplement);
        Assert.NotNull(stackOptions);
    }

    [Fact(Skip = "Changing the expected assemblies breaks other tests when collections don't work as expected")]
    public void Throws_When_ConnectionType_NotFound()
    {
        var microsoftAssemblies = RedisTypeLocator.MicrosoftAssemblies;
        var stackAssemblies = RedisTypeLocator.StackExchangeAssemblies;
        RedisTypeLocator.MicrosoftAssemblies = new[] { "something-Wrong" };
        RedisTypeLocator.StackExchangeAssemblies = new[] { "something-Wrong" };

        var microsoftException = Assert.Throws<ConnectorException>(() => RedisTypeLocator.MicrosoftInterface);
        var stackException = Assert.Throws<ConnectorException>(() => RedisTypeLocator.StackExchangeInterface);

        Assert.Equal($"Unable to find {RedisTypeLocator.MicrosoftInterfaceTypeNames[0]}, are you missing a Microsoft Caching NuGet Reference?", microsoftException.Message);
        Assert.Equal($"Unable to find {RedisTypeLocator.StackExchangeInterfaceTypeNames[0]}, are you missing a Stack Exchange Redis NuGet Reference?", stackException.Message);

        // reset
        RedisTypeLocator.MicrosoftAssemblies = microsoftAssemblies;
        RedisTypeLocator.StackExchangeAssemblies = stackAssemblies;
    }
}

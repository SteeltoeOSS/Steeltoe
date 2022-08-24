// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connector.CosmosDb;
using Xunit;

namespace Steeltoe.Connector.MongoDb.Test;

public class MongoDbTypeLocatorTest
{
    [Fact]
    public void Property_Can_Locate_ConnectionTypes()
    {
        // arrange -- handled by including a compatible MongoDB NuGet package
        Type interfaceType = MongoDbTypeLocator.MongoClientInterface;
        Type implementationType = MongoDbTypeLocator.MongoClient;
        Type mongoUrlType = MongoDbTypeLocator.MongoUrl;

        Assert.NotNull(interfaceType);
        Assert.NotNull(implementationType);
        Assert.NotNull(mongoUrlType);
    }

    [Fact]
    public void Throws_When_ConnectionType_NotFound()
    {
        string[] types = MongoDbTypeLocator.ConnectionInterfaceTypeNames;

        MongoDbTypeLocator.ConnectionInterfaceTypeNames = new[]
        {
            "something-Wrong"
        };

        var exception = Assert.Throws<TypeLoadException>(() => MongoDbTypeLocator.MongoClientInterface);

        Assert.Equal("Unable to find IMongoClient, are you missing a MongoDB driver?", exception.Message);

        // reset
        MongoDbTypeLocator.ConnectionInterfaceTypeNames = types;
    }
}

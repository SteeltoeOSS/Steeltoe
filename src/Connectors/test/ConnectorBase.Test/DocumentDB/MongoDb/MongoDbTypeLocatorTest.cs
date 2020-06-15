// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Steeltoe.Connector.MongoDb.Test
{
    public class MongoDbTypeLocatorTest
    {
        [Fact]
        public void Property_Can_Locate_ConnectionTypes()
        {
            // arrange -- handled by including a compatible MongoDB NuGet package

            // act
            var interfaceType = MongoDbTypeLocator.IMongoClient;
            var implementationType = MongoDbTypeLocator.MongoClient;
            var mongoUrlType = MongoDbTypeLocator.MongoUrl;

            // assert
            Assert.NotNull(interfaceType);
            Assert.NotNull(implementationType);
            Assert.NotNull(mongoUrlType);
        }

        [Fact]
        public void Throws_When_ConnectionType_NotFound()
        {
            // arrange
            var types = MongoDbTypeLocator.ConnectionInterfaceTypeNames;
            MongoDbTypeLocator.ConnectionInterfaceTypeNames = new string[] { "something-Wrong" };

            // act
            var exception = Assert.Throws<TypeLoadException>(() => MongoDbTypeLocator.IMongoClient);

            // assert
            Assert.Equal("Unable to find IMongoClient, are you missing a MongoDB driver?", exception.Message);

            // reset
            MongoDbTypeLocator.ConnectionInterfaceTypeNames = types;
        }
    }
}

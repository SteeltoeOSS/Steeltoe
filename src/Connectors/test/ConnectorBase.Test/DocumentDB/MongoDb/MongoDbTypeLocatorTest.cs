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

using System;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.MongoDb.Test
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

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

namespace Steeltoe.CloudFoundry.Connector.PostgreSql.Test
{
    public class PostgreSqlTypeLocatorTest
    {
        [Fact]
        public void Property_Can_Locate_ConnectionType()
        {
            // arrange -- handled by including a compatible PostgreSql NuGet package

            // act
            var type = PostgreSqlTypeLocator.NpgsqlConnection;

            // assert
            Assert.NotNull(type);
        }

        [Fact]
        public void Throws_When_ConnectionType_NotFound()
        {
            // arrange
            var types = PostgreSqlTypeLocator.ConnectionTypeNames;
            PostgreSqlTypeLocator.ConnectionTypeNames = new string[] { "something-Wrong" };

            // act
            var exception = Assert.Throws<TypeLoadException>(() => PostgreSqlTypeLocator.NpgsqlConnection);

            // assert
            Assert.Equal("Unable to find NpgsqlConnection, are you missing a PostgreSQL ADO.NET assembly?", exception.Message);

            // reset
            PostgreSqlTypeLocator.ConnectionTypeNames = types;
        }
    }
}

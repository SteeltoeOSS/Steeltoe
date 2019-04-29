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

namespace Steeltoe.CloudFoundry.Connector.SqlServer.Test
{
    public class SqlServerTypeLocatorTest
    {
        [Fact]
        public void Property_Can_Locate_ConnectionType()
        {
            // arrange -- handled by including System.Data.SqlClient

            // act
            var type = SqlServerTypeLocator.SqlConnection;

            // assert
            Assert.NotNull(type);
        }

        [Fact]
        public void Throws_When_ConnectionType_NotFound()
        {
            // arrange
            var types = SqlServerTypeLocator.ConnectionTypeNames;
            SqlServerTypeLocator.ConnectionTypeNames = new string[] { "something-Wrong" };

            // act
            var exception = Assert.Throws<ConnectorException>(() => SqlServerTypeLocator.SqlConnection);

            // assert
            Assert.Equal("Unable to find SqlConnection, are you missing a Microsoft SQL Server ADO.NET assembly?", exception.Message);

            // reset
            SqlServerTypeLocator.ConnectionTypeNames = types;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            var exception = Assert.Throws<ConnectorException>(() => PostgreSqlTypeLocator.NpgsqlConnection);

            // assert
            Assert.Equal("Unable to find NpgsqlConnection, are you missing a PostgreSQL ADO.NET assembly?", exception.Message);

            // reset
            PostgreSqlTypeLocator.ConnectionTypeNames = types;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Steeltoe.Connector.PostgreSql.Test;

public class PostgreSqlTypeLocatorTest
{
    [Fact]
    public void Property_Can_Locate_ConnectionType()
    {
        // arrange -- handled by including a compatible PostgreSql NuGet package
        var type = PostgreSqlTypeLocator.NpgsqlConnection;

        Assert.NotNull(type);
    }

    [Fact]
    public void Throws_When_ConnectionType_NotFound()
    {
        var types = PostgreSqlTypeLocator.ConnectionTypeNames;
        PostgreSqlTypeLocator.ConnectionTypeNames = new string[] { "something-Wrong" };

        var exception = Assert.Throws<TypeLoadException>(() => PostgreSqlTypeLocator.NpgsqlConnection);

        Assert.Equal("Unable to find NpgsqlConnection, are you missing a PostgreSQL ADO.NET assembly?", exception.Message);

        // reset
        PostgreSqlTypeLocator.ConnectionTypeNames = types;
    }
}
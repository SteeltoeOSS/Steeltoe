// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Steeltoe.Connector.SqlServer.Test;

public class SqlServerTypeLocatorTest
{
    [Fact]
    public void Property_Can_Locate_ConnectionType()
    {
        // arrange -- handled by including System.Data.SqlClient
        var type = SqlServerTypeLocator.SqlConnection;

        Assert.NotNull(type);
    }

    [Fact]
    public void Throws_When_ConnectionType_NotFound()
    {
        var types = SqlServerTypeLocator.ConnectionTypeNames;
        SqlServerTypeLocator.ConnectionTypeNames = new string[] { "something-Wrong" };

        var exception = Assert.Throws<TypeLoadException>(() => SqlServerTypeLocator.SqlConnection);

        Assert.Equal("Unable to find SqlConnection, are you missing a Microsoft SQL Server ADO.NET assembly?", exception.Message);

        // reset
        SqlServerTypeLocator.ConnectionTypeNames = types;
    }
}
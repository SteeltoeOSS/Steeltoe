// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Steeltoe.Connectors.SqlServer;
using Xunit;

namespace Steeltoe.Connectors.Test.SqlServer;

public class SqlServerTypeLocatorTest
{
    [Fact]
    public void Driver_Found_In_System_Assembly()
    {
        string[] backupAssemblies = SqlServerTypeLocator.Assemblies;
        string[] backupTypes = SqlServerTypeLocator.ConnectionTypeNames;

        try
        {
            SqlServerTypeLocator.Assemblies = new[]
            {
                "System.Data.SqlClient"
            };

            SqlServerTypeLocator.ConnectionTypeNames = new[]
            {
                "System.Data.SqlClient.SqlConnection"
            };

            Type type = SqlServerTypeLocator.SqlConnection;
            type.Should().NotBeNull();
        }
        finally
        {
            SqlServerTypeLocator.Assemblies = backupAssemblies;
            SqlServerTypeLocator.ConnectionTypeNames = backupTypes;
        }
    }

    [Fact]
    public void Driver_Found_In_Microsoft_Assembly()
    {
        string[] backupAssemblies = SqlServerTypeLocator.Assemblies;
        string[] backupTypes = SqlServerTypeLocator.ConnectionTypeNames;

        try
        {
            SqlServerTypeLocator.Assemblies = new[]
            {
                "Microsoft.Data.SqlClient"
            };

            SqlServerTypeLocator.ConnectionTypeNames = new[]
            {
                "Microsoft.Data.SqlClient.SqlConnection"
            };

            Type type = SqlServerTypeLocator.SqlConnection;
            type.Should().NotBeNull();
        }
        finally
        {
            SqlServerTypeLocator.Assemblies = backupAssemblies;
            SqlServerTypeLocator.ConnectionTypeNames = backupTypes;
        }
    }

    [Fact]
    public void Throws_When_ConnectionType_NotFound()
    {
        string[] types = SqlServerTypeLocator.ConnectionTypeNames;

        SqlServerTypeLocator.ConnectionTypeNames = new[]
        {
            "something-Wrong"
        };

        var exception = Assert.Throws<TypeLoadException>(() => SqlServerTypeLocator.SqlConnection);

        Assert.Equal("Unable to find SqlConnection, are you missing a Microsoft SQL Server ADO.NET assembly?", exception.Message);

        // reset
        SqlServerTypeLocator.ConnectionTypeNames = types;
    }
}

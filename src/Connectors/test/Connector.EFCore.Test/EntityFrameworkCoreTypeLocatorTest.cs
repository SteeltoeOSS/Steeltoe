// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Connector.EFCore.Test;

public class EntityFrameworkCoreTypeLocatorTest
{
    [Fact]
    public void Property_Can_Locate_MySqlDbContextOptionsType_MySql()
    {
        var assemblies = EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies;
        EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies = new [] { "MySql.EntityFrameworkCore" };

        var type = EntityFrameworkCoreTypeLocator.MySqlDbContextOptionsType;

        Assert.NotNull(type);
        EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies = assemblies;
    }

    [Fact]
    public void Property_Can_Locate_MySqlDbContextOptionsType_Pomelo()
    {
        var assemblies = EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies;
        EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies = new [] { "Pomelo.EntityFrameworkCore.MySql" };

        var type = EntityFrameworkCoreTypeLocator.MySqlDbContextOptionsType;

        Assert.NotNull(type);
        EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies = assemblies;
    }

    [Fact]
    public void Property_Can_Locate_PostgreSqlDbContextOptionsType()
    {
        // arrange -- handled by including a compatible EF Core NuGet package
        var type = EntityFrameworkCoreTypeLocator.PostgreSqlDbContextOptionsType;

        Assert.NotNull(type);
    }

    [Fact]
    public void Property_Can_Locate_SqlServerDbContextOptionsType()
    {
        // arrange -- handled by including a compatible EF Core NuGet package
        var type = EntityFrameworkCoreTypeLocator.SqlServerDbContextOptionsType;

        Assert.NotNull(type);
    }

    [Fact]
    public void Options_Found_In_OracleEF_Assembly()
    {
        // arrange ~ narrow the assembly list to one specific nuget package
        var oracleAssemblies = EntityFrameworkCoreTypeLocator.OracleEntityAssemblies;
        EntityFrameworkCoreTypeLocator.OracleEntityAssemblies = new[] { EntityFrameworkCoreTypeLocator.OracleEntityAssemblies[0] };

        var type = EntityFrameworkCoreTypeLocator.OracleDbContextOptionsType;

        Assert.NotNull(type);
        EntityFrameworkCoreTypeLocator.OracleEntityAssemblies = oracleAssemblies;
    }

    [Fact(Skip = "Change NuGet reference to see this test pass")]
    public void Options_Found_In_Devart_Assembly()
    {
        // arrange ~ narrow the assembly list to one specific nuget package
        var oracleAssemblies = EntityFrameworkCoreTypeLocator.OracleEntityAssemblies;
        EntityFrameworkCoreTypeLocator.OracleEntityAssemblies = new[] { EntityFrameworkCoreTypeLocator.OracleEntityAssemblies[1] };

        var type = EntityFrameworkCoreTypeLocator.OracleDbContextOptionsType;

        Assert.NotNull(type);
        EntityFrameworkCoreTypeLocator.OracleEntityAssemblies = oracleAssemblies;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Connectors.EntityFrameworkCore.Test;

public class EntityFrameworkCoreTypeLocatorTest
{
    [Fact]
    public void Property_Can_Locate_MySqlDbContextOptionsType_MySql()
    {
        string[] assemblies = EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies;

        EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies = new[]
        {
            "MySql.EntityFrameworkCore"
        };

        Type type = EntityFrameworkCoreTypeLocator.MySqlDbContextOptionsType;

        Assert.NotNull(type);
        EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies = assemblies;
    }

    [Fact]
    public void Property_Can_Locate_MySqlDbContextOptionsType_Pomelo()
    {
        string[] assemblies = EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies;

        EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies = new[]
        {
            "Pomelo.EntityFrameworkCore.MySql"
        };

        Type type = EntityFrameworkCoreTypeLocator.MySqlDbContextOptionsType;

        Assert.NotNull(type);
        EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies = assemblies;
    }

    [Fact]
    public void Property_Can_Locate_PostgreSqlDbContextOptionsType()
    {
        // arrange -- handled by including a compatible EF Core NuGet package
        Type type = EntityFrameworkCoreTypeLocator.PostgreSqlDbContextOptionsType;

        Assert.NotNull(type);
    }

    [Fact]
    public void Property_Can_Locate_SqlServerDbContextOptionsType()
    {
        // arrange -- handled by including a compatible EF Core NuGet package
        Type type = EntityFrameworkCoreTypeLocator.SqlServerDbContextOptionsType;

        Assert.NotNull(type);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Connector.EFCore.Test
{
    public class EntityFrameworkCoreTypeLocatorTest
    {
        [Fact]
        public void Property_Can_Locate_MySqlDbContextOptionsType()
        {
            // arrange -- handled by including a compatible EF Core NuGet package

            // act
            var type = EntityFrameworkCoreTypeLocator.MySqlDbContextOptionsType;

            // assert
            Assert.NotNull(type);
        }

#if NETCOREAPP3_1
        [Fact]
        public void Options_Found_In_MySql_Assembly()
        {
            // arrange ~ narrow the assembly list to one specific nuget package
            var types = EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies;
            EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies = new string[] { "MySql.EntityFrameworkCore" };

            // act
            var type = EntityFrameworkCoreTypeLocator.MySqlDbContextOptionsType;

            // assert
            Assert.NotNull(type);
            EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies = types;
        }
#else
        [Fact]
        public void Options_Found_In_Pomelo_Assembly()
        {
            // arrange ~ narrow the assembly list to one specific nuget package
            var types = EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies;
            EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies = new string[] { "Pomelo.EntityFrameworkCore.MySql" };

            // act
            var type = EntityFrameworkCoreTypeLocator.MySqlDbContextOptionsType;

            // assert
            Assert.NotNull(type);
            EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies = types;
        }
#endif

        [Fact]
        public void Property_Can_Locate_PostgreSqlDbContextOptionsType()
        {
            // arrange -- handled by including a compatible EF Core NuGet package

            // act
            var type = EntityFrameworkCoreTypeLocator.PostgreSqlDbContextOptionsType;

            // assert
            Assert.NotNull(type);
        }

        [Fact]
        public void Property_Can_Locate_SqlServerDbContextOptionsType()
        {
            // arrange -- handled by including a compatible EF Core NuGet package

            // act
            var type = EntityFrameworkCoreTypeLocator.SqlServerDbContextOptionsType;

            // assert
            Assert.NotNull(type);
        }

        [Fact]
        public void Options_Found_In_OracleEF_Assembly()
        {
            // arrange ~ narrow the assembly list to one specific nuget package
            var oracleAssemblies = EntityFrameworkCoreTypeLocator.OracleEntityAssemblies;
            EntityFrameworkCoreTypeLocator.OracleEntityAssemblies = new string[] { EntityFrameworkCoreTypeLocator.OracleEntityAssemblies[0] };

            // act
            var type = EntityFrameworkCoreTypeLocator.OracleDbContextOptionsType;

            // assert
            Assert.NotNull(type);
            EntityFrameworkCoreTypeLocator.OracleEntityAssemblies = oracleAssemblies;
        }

        [Fact(Skip = "Change NuGet reference to see this test pass")]
        public void Options_Found_In_Devart_Assembly()
        {
            // arrange ~ narrow the assembly list to one specific nuget package
            var oracleAssemblies = EntityFrameworkCoreTypeLocator.OracleEntityAssemblies;
            EntityFrameworkCoreTypeLocator.OracleEntityAssemblies = new string[] { EntityFrameworkCoreTypeLocator.OracleEntityAssemblies[1] };

            // act
            var type = EntityFrameworkCoreTypeLocator.OracleDbContextOptionsType;

            // assert
            Assert.NotNull(type);
            EntityFrameworkCoreTypeLocator.OracleEntityAssemblies = oracleAssemblies;
        }
    }
}

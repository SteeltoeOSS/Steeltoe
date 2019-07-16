// Copyright 2019 Infosys Ltd.
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

namespace Steeltoe.CloudFoundry.Connector.EFCore.Test
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

        [Fact(Skip = "Change NuGet reference to see this test pass")]
        public void Options_Found_In_Oracle_Assembly()
        {
            // arrange ~ narrow the assembly list to one specific nuget package
            var types = EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies;
            EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies = new string[] { "MySql.Data.EntityFrameworkCore" };

            // act
            var type = EntityFrameworkCoreTypeLocator.MySqlDbContextOptionsType;

            // assert
            Assert.NotNull(type);
            EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies = types;
        }

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

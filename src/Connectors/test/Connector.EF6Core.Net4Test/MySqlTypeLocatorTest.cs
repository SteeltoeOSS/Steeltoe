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

namespace Steeltoe.CloudFoundry.Connector.MySql.Test
{
    /// <summary>
    /// These tests can be found in Base, EF6 Autofac, EF6 Core and EF Core, for testing different nuget packages.
    /// This version should be testing the v8+ line of the Oracle driver, brought in by v8+ of MySql.Data.EntityFramework
    /// Don't remove it unless you've got a better idea for making sure we work with multiple assemblies
    /// with conflicting names/types
    /// </summary>
    public class MySqlTypeLocatorTest
    {
        [Fact]
        public void Property_Can_Locate_ConnectionType()
        {
            // arrange -- handled by including a compatible MySql NuGet package

            // act
            var type = MySqlTypeLocator.MySqlConnection;

            // assert
            Assert.NotNull(type);
        }

        [Fact]
        public void Driver_Found_In_MySqlData_Assembly()
        {
            // arrange ~ narrow the assembly list to one specific nuget package
            var removedAssembly = MySqlTypeLocator.Assemblies[1];
            MySqlTypeLocator.Assemblies[1] = string.Empty;

            // act
            var type = MySqlTypeLocator.MySqlConnection;

            // assert
            Assert.NotNull(type);
            MySqlTypeLocator.Assemblies[1] = removedAssembly;
        }
    }
}

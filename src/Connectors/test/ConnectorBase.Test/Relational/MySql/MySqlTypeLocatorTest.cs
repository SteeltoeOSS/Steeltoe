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

using System;
using System.Linq;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.MySql.Test
{
    /// <summary>
    /// These tests can be found in Base, EF6 Autofac, EF6 Core and EF Core, for testing different nuget packages.
    /// This version should be testing the MySqlConnector driver
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
        public void Driver_Found_In_MySqlConnector_Assembly()
        {
            // arrange ~ narrow the assembly list to one specific nuget package
            var types = MySqlTypeLocator.ConnectionTypeNames;
            MySqlTypeLocator.Assemblies = new string[] { "MySqlConnector" };

            // act
            var type = MySqlTypeLocator.MySqlConnection;

            // assert
            Assert.NotNull(type);
            MySqlTypeLocator.ConnectionTypeNames = types;
        }

        [Fact]
        public void Throws_When_ConnectionType_NotFound()
        {
            // arrange
            var types = MySqlTypeLocator.ConnectionTypeNames;
            MySqlTypeLocator.ConnectionTypeNames = new string[] { "something-Wrong" };

            // act
            var exception = Assert.Throws<TypeLoadException>(() => MySqlTypeLocator.MySqlConnection);

            // assert
            Assert.Equal("Unable to find MySqlConnection, are you missing a MySql ADO.NET assembly?", exception.Message);

            // reset
            MySqlTypeLocator.ConnectionTypeNames = types;
        }
    }
}

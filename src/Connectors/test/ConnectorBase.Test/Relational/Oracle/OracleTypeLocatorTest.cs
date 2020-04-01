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
using Xunit;

namespace Steeltoe.Connector.Oracle.Test
{
    public class OracleTypeLocatorTest
    {
        [Fact]
        public void Property_Can_Locate_ConnectionType()
        {
            // arrange -- handled by including a compatible Oracle NuGet package

            // act
            var type = OracleTypeLocator.OracleConnection;

            // assert
            Assert.NotNull(type);
        }

        [Fact]
        public void Driver_Found_In_ODPNet_Assembly()
        {
            // arrange ~ narrow the assembly list to one specific nuget package
            var assemblies = OracleTypeLocator.Assemblies;
            OracleTypeLocator.Assemblies = new string[] { "Oracle.ManagedDataAccess" };

            // act
            var type = OracleTypeLocator.OracleConnection;

            // assert
            Assert.NotNull(type);
            OracleTypeLocator.Assemblies = assemblies;
        }

        [Fact]
        public void Throws_When_ConnectionType_NotFound()
        {
            // arrange
            var types = OracleTypeLocator.ConnectionTypeNames;
            OracleTypeLocator.ConnectionTypeNames = new string[] { "something-Wrong" };

            // act
            var exception = Assert.Throws<TypeLoadException>(() => OracleTypeLocator.OracleConnection);

            // assert
            Assert.Equal("Unable to find OracleConnection, are you missing a Oracle ODP.NET assembly?", exception.Message);

            // reset
            OracleTypeLocator.ConnectionTypeNames = types;
        }
    }
}

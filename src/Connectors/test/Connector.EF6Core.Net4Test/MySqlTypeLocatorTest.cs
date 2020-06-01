// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET461
using Steeltoe.CloudFoundry.Connector.MySql;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.EF6Core.MySql.Test
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
            var types = MySqlTypeLocator.Assemblies;
            MySqlTypeLocator.Assemblies = new string[] { "MySql.Data" };

            // act
            var type = MySqlTypeLocator.MySqlConnection;

            // assert
            Assert.NotNull(type);
            MySqlTypeLocator.Assemblies = types;
        }
    }
}
#endif
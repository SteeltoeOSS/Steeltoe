// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Steeltoe.Connector.MySql.Test;

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
        var type = MySqlTypeLocator.MySqlConnection;

        Assert.NotNull(type);
    }

    [Fact]
    public void Driver_Found_In_MySqlConnector_Assembly()
    {
        // arrange ~ narrow the assembly list to one specific nuget package
        var types = MySqlTypeLocator.ConnectionTypeNames;
        MySqlTypeLocator.Assemblies = new[] { "MySqlConnector" };

        var type = MySqlTypeLocator.MySqlConnection;

        Assert.NotNull(type);
        MySqlTypeLocator.ConnectionTypeNames = types;
    }

    [Fact]
    public void Throws_When_ConnectionType_NotFound()
    {
        var types = MySqlTypeLocator.ConnectionTypeNames;
        MySqlTypeLocator.ConnectionTypeNames = new[] { "something-Wrong" };

        var exception = Assert.Throws<TypeLoadException>(() => MySqlTypeLocator.MySqlConnection);

        Assert.Equal("Unable to find MySqlConnection, are you missing a MySql ADO.NET assembly?", exception.Message);

        // reset
        MySqlTypeLocator.ConnectionTypeNames = types;
    }
}

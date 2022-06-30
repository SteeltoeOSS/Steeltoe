// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Connector.MySql.Test;

/// <summary>
/// These tests can be found in Base, EF6 Autofac, EF6 Core and EF Core, for testing different nuget packages.
/// This version should be testing the driver brought in by the Pomelo EF Core package
/// Don't remove it unless you've got a better idea for making sure we work with multiple assemblies
/// with conflicting names/types.
/// </summary>
public partial class MySqlTypeLocatorTest
{
    [Fact]
    public void Property_Can_Locate_ConnectionType()
    {
        // arrange -- handled by including a compatible MySql NuGet package
        var type = MySqlTypeLocator.MySqlConnection;

        Assert.NotNull(type);
    }
}

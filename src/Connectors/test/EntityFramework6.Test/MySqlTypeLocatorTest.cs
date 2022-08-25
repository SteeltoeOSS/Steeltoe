// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connector.MySql;
using Xunit;

namespace Steeltoe.Connector.EntityFramework6.MySql.Test;

public class MySqlTypeLocatorTest
{
    [Fact]
    public void Property_Can_Locate_ConnectionType()
    {
        // arrange -- handled by including a compatible MySql NuGet package
        Type type = MySqlTypeLocator.MySqlConnection;

        Assert.NotNull(type);
    }

    [Fact]
    public void Driver_Found_In_MySqlData_Assembly()
    {
        // arrange ~ narrow the assembly list to one specific nuget package
        string[] types = MySqlTypeLocator.Assemblies;

        MySqlTypeLocator.Assemblies = new[]
        {
            "MySql.Data"
        };

        Type type = MySqlTypeLocator.MySqlConnection;

        Assert.NotNull(type);
        MySqlTypeLocator.Assemblies = types;
    }
}

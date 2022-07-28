// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Connector.MySql.Test;

public class MySqlTypeLocatorTest
{
    [Fact]
    public void Driver_Found_In_MySqlConnector_Assembly()
    {
        var types = MySqlTypeLocator.Assemblies;
        MySqlTypeLocator.Assemblies = new[] { "MySqlConnector" };

        var type = MySqlTypeLocator.MySqlConnection;

        Assert.NotNull(type);
        MySqlTypeLocator.Assemblies = types;
    }

    [Fact]
    public void Driver_Found_In_MySqlData_Assembly()
    {
        var types = MySqlTypeLocator.Assemblies;
        MySqlTypeLocator.Assemblies = new[] { "MySql.Data" };

        var type = MySqlTypeLocator.MySqlConnection;

        Assert.NotNull(type);
        MySqlTypeLocator.Assemblies = types;
    }
}

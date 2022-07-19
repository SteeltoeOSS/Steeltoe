// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using Xunit;

namespace Steeltoe.Connector.MySql.Test;

public partial class MySqlTypeLocatorTest
{
    [Fact]
    public void Driver_Found_In_MySqlConnector_Assembly()
    {
        // arrange ~ narrow the assembly list to one specific nuget package
        var types = MySqlTypeLocator.Assemblies;
        MySqlTypeLocator.Assemblies = new[] { "MySqlConnector" };

        var type = MySqlTypeLocator.MySqlConnection;

        Assert.NotNull(type);
        MySqlTypeLocator.Assemblies = types;
    }
}
#endif

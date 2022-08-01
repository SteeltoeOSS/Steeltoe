// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Connector.Oracle.Test;

public class OracleTypeLocatorTest
{
    [Fact]
    public void Property_Can_Locate_ConnectionType()
    {
        // arrange -- handled by including a compatible Oracle NuGet package
        var type = OracleTypeLocator.OracleConnection;

        Assert.NotNull(type);
    }

    [Fact]
    public void Driver_Found_In_ODPNet_Assembly()
    {
        // arrange ~ narrow the assembly list to one specific nuget package
        var assemblies = OracleTypeLocator.Assemblies;
        OracleTypeLocator.Assemblies = new[] { "Oracle.ManagedDataAccess" };

        var type = OracleTypeLocator.OracleConnection;

        Assert.NotNull(type);
        OracleTypeLocator.Assemblies = assemblies;
    }

    [Fact]
    public void Throws_When_ConnectionType_NotFound()
    {
        var types = OracleTypeLocator.ConnectionTypeNames;
        OracleTypeLocator.ConnectionTypeNames = new[] { "something-Wrong" };

        var exception = Assert.Throws<TypeLoadException>(() => OracleTypeLocator.OracleConnection);

        Assert.Equal("Unable to find OracleConnection, are you missing a Oracle ODP.NET assembly?", exception.Message);

        // reset
        OracleTypeLocator.ConnectionTypeNames = types;
    }
}

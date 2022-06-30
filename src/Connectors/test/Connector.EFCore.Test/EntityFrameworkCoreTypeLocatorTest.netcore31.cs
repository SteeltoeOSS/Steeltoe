// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NETCOREAPP3_1
using Xunit;

namespace Steeltoe.Connector.EFCore.Test;

public partial class EntityFrameworkCoreTypeLocatorTest
{
    [Fact]
    public void Options_Found_In_MySql_Assembly()
    {
        // arrange ~ narrow the assembly list to one specific nuget package
        var types = EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies;
        EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies = new[] { "MySql.EntityFrameworkCore" };

        var type = EntityFrameworkCoreTypeLocator.MySqlDbContextOptionsType;

        Assert.NotNull(type);
        EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies = types;
    }
}
#endif

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using Xunit;

namespace Steeltoe.Connector.EFCore.Test;

public partial class EntityFrameworkCoreTypeLocatorTest
{
    [Fact]
    public void Options_Found_In_Pomelo_Assembly()
    {
        // arrange ~ narrow the assembly list to one specific nuget package
        var types = EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies;
        EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies = new[] { "Pomelo.EntityFrameworkCore.MySql" };

        var type = EntityFrameworkCoreTypeLocator.MySqlDbContextOptionsType;

        Assert.NotNull(type);
        EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies = types;
    }
}
#endif

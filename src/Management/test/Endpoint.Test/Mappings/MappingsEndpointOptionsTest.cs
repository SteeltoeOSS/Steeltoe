// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.RouteMappings;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Mappings;

public sealed class MappingsEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var opts = GetOptionsFromSettings<RouteMappingsEndpointOptions>();
        Assert.Null(opts.Enabled);
        Assert.Equal("mappings", opts.Id);
        Assert.Equal(Permissions.Restricted, opts.RequiredPermissions);
    }
}

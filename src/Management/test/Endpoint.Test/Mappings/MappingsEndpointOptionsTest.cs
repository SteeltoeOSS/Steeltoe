// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Test;
using Xunit;

namespace Steeltoe.Management.Endpoint.Mappings.Test;

public class MappingsEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var opts = new MappingsEndpointOptions();
        Assert.Null(opts.Enabled);
        Assert.Equal("mappings", opts.Id);
        Assert.Equal(Permissions.Restricted, opts.RequiredPermissions);
    }

    [Fact]
    public void Constructor_ThrowsIfConfigNull()
    {
        const IConfiguration configuration = null;
        Assert.Throws<ArgumentNullException>(() => new MappingsEndpointOptions(configuration));
    }
}

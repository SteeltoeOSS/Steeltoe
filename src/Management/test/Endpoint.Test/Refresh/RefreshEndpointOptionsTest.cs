// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Refresh;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Refresh;

public class RefreshEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var opts = GetOptionsFromSettings<RefreshEndpointOptions>();
        Assert.Null(opts.Enabled);
        Assert.Equal("refresh", opts.Id);
        Assert.Equal(Permissions.Restricted, opts.RequiredPermissions);
        Assert.True(opts.ReturnConfiguration);
    }
}

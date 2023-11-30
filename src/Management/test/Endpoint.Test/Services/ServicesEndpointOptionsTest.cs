// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Services;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Services;

public class ServicesEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var opts = GetOptionsFromSettings<ServicesEndpointOptions>();
        Assert.Null(opts.Enabled);
        Assert.Equal("beans", opts.Id);
        Assert.Equal(Permissions.Restricted, opts.RequiredPermissions);
    }
}

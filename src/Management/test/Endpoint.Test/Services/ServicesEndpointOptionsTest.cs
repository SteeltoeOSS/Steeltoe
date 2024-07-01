// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Services;

namespace Steeltoe.Management.Endpoint.Test.Services;

public sealed class ServicesEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var options = GetOptionsFromSettings<ServicesEndpointOptions>();
        Assert.Null(options.Enabled);
        Assert.Equal("beans", options.Id);
        Assert.Equal(Permissions.Restricted, options.RequiredPermissions);
    }
}

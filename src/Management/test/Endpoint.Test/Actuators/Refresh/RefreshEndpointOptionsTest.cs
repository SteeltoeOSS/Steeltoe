// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.Refresh;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Refresh;

public sealed class RefreshEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var options = GetOptionsFromSettings<RefreshEndpointOptions>();
        Assert.Null(options.Enabled);
        Assert.Equal("refresh", options.Id);
        Assert.Equal(Permissions.Restricted, options.RequiredPermissions);
        Assert.True(options.ReturnConfiguration);
    }
}

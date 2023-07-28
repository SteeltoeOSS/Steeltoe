// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Environment;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Environment;

public sealed class EnvironmentEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        EnvironmentEndpointOptions options = GetOptionsFromSettings<EnvironmentEndpointOptions, ConfigureEnvironmentEndpointOptions>();
        Assert.Equal("env", options.Id);

        Assert.Equal(new[]
        {
            "password",
            "secret",
            "key",
            "token",
            ".*credentials.*",
            "vcap_services"
        }, options.KeysToSanitize);

        Assert.Equal(Permissions.Restricted, options.RequiredPermissions);
    }
}

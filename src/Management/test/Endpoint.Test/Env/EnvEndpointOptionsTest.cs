// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Env;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Env;

public class EnvEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        EnvEndpointOptions opts = GetOptionsFromSettings<EnvEndpointOptions, ConfigureEnvEndpointOptions>();
        Assert.Equal("env", opts.Id);

        Assert.Equal(new[]
        {
            "password",
            "secret",
            "key",
            "token",
            ".*credentials.*",
            "vcap_services"
        }, opts.KeysToSanitize);

        Assert.Equal(Permissions.Restricted, opts.RequiredPermissions);
    }
}

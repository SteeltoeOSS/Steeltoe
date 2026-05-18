// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Env.Test;

public class EnvEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var opts = new EnvEndpointOptions();
        Assert.Equal("env", opts.Id);
        Assert.Equal(new string[] { "password", "secret", "key", "token", ".*credentials.*", "vcap_services", ".*connectionstring.*" }, opts.KeysToSanitize);
        Assert.Equal(Permissions.FULL, opts.RequiredPermissions);
    }

    [Fact]
    public void Constructor_ThrowsIfConfigNull()
    {
        IConfiguration config = null;
        Assert.Throws<ArgumentNullException>(() => new EnvEndpointOptions(config));
    }

    [Fact]
    public void Constructor_BindsRequiredPermissions_FromConfig()
    {
        var appsettings = new Dictionary<string, string>()
        {
            ["management:endpoints:env:requiredPermissions"] = "RESTRICTED"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();

        var opts = new EnvEndpointOptions(config);

        Assert.Equal(Permissions.RESTRICTED, opts.RequiredPermissions);
    }
}
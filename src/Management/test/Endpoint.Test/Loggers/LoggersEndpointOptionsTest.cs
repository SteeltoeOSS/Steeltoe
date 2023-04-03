// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Loggers;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Loggers;

public class LoggersEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var opts = GetOptionsFromSettings<LoggersEndpointOptions>();
        Assert.Null(opts.Enabled);
        Assert.Equal("loggers", opts.Id);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:loggers:enabled"] = "false",
            ["management:endpoints:cloudfoundry:validatecertificates"] = "true",
            ["management:endpoints:cloudfoundry:enabled"] = "true"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);

        var opts = GetOptionsFromSettings<LoggersEndpointOptions>(appsettings);
        var cloudOpts = GetOptionsFromSettings<CloudFoundryEndpointOptions>(appsettings);

        Assert.True(cloudOpts.Enabled);
        Assert.Equal(string.Empty, cloudOpts.Id);
        Assert.Equal(string.Empty, cloudOpts.Path);
        Assert.True(cloudOpts.ValidateCertificates);

        Assert.False(opts.Enabled);
        Assert.Equal("loggers", opts.Id);
        Assert.Equal("loggers", opts.Path);
    }
}

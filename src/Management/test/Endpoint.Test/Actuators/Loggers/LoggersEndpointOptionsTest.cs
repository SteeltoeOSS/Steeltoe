// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Actuators.Loggers;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Loggers;

public sealed class LoggersEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var options = GetOptionsFromSettings<LoggersEndpointOptions>();
        Assert.Null(options.Enabled);
        Assert.Equal("loggers", options.Id);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appsettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:loggers:enabled"] = "false",
            ["management:endpoints:cloudfoundry:validatecertificates"] = "true",
            ["management:endpoints:cloudfoundry:enabled"] = "true"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);

        var loggersEndpointOptions = GetOptionsFromSettings<LoggersEndpointOptions>(appsettings);
        var cloudFoundryEndpointOptions = GetOptionsFromSettings<CloudFoundryEndpointOptions>(appsettings);

        Assert.True(cloudFoundryEndpointOptions.Enabled);
        Assert.Equal(string.Empty, cloudFoundryEndpointOptions.Id);
        Assert.Equal(string.Empty, cloudFoundryEndpointOptions.Path);
        Assert.True(cloudFoundryEndpointOptions.ValidateCertificates);

        Assert.False(loggersEndpointOptions.Enabled);
        Assert.Equal("loggers", loggersEndpointOptions.Id);
        Assert.Equal("loggers", loggersEndpointOptions.Path);
    }
}

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
        options.Enabled.Should().BeNull();
        options.Id.Should().Be("loggers");
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:loggers:enabled"] = "false",
            ["management:endpoints:cloudfoundry:validateCertificates"] = "true",
            ["management:endpoints:cloudfoundry:enabled"] = "true"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);

        var loggersEndpointOptions = GetOptionsFromSettings<LoggersEndpointOptions>(appSettings);
        var cloudFoundryEndpointOptions = GetOptionsFromSettings<CloudFoundryEndpointOptions>(appSettings);

        cloudFoundryEndpointOptions.Enabled.Should().BeTrue();
        cloudFoundryEndpointOptions.Id.Should().BeEmpty();
        cloudFoundryEndpointOptions.Path.Should().BeEmpty();
        cloudFoundryEndpointOptions.ValidateCertificates.Should().BeTrue();

        loggersEndpointOptions.Enabled.Should().BeFalse();
        loggersEndpointOptions.Id.Should().Be("loggers");
        loggersEndpointOptions.Path.Should().Be("loggers");
    }
}

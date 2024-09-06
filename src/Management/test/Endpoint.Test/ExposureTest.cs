// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test;

public sealed class ExposureTest : BaseTest
{
    [Fact]
    public void ExposureReturnsDefaults()
    {
        var options = GetOptionsFromSettings<ManagementOptions>();

        options.Exposure.Include.Should().HaveCount(2);
        options.Exposure.Include.Should().Contain("health");
        options.Exposure.Include.Should().Contain("info");

        options.Exposure.Exclude.Should().BeEmpty();
    }

    [Fact]
    public void ExposureDoesNotBindToDefaultOptions()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:exposure:include:0"] = "httptrace",
            ["management:endpoints:exposure:include:1"] = "dbmigrations",
            ["management:endpoints:exposure:exclude:0"] = "trace",
            ["management:endpoints:exposure:exclude:1"] = "env"
        };

        var options = GetOptionsFromSettings<ManagementOptions>(appSettings);

        options.Exposure.Include.Should().HaveCount(2);
        options.Exposure.Include.Should().Contain("health");
        options.Exposure.Include.Should().Contain("info");

        options.Exposure.Exclude.Should().BeEmpty();
    }

    [Fact]
    public void ExposureCanClearDefaults()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:actuator:exposure:include:0"] = string.Empty,
            ["management:endpoints:actuator:exposure:exclude:0"] = string.Empty
        };

        var options = GetOptionsFromSettings<ManagementOptions>(appSettings);

        options.Exposure.Include.Should().BeEmpty();
        options.Exposure.Exclude.Should().BeEmpty();
    }

    [Fact]
    public void ExposureBindsToSteeltoeSettings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:actuator:exposure:include:0"] = "httptrace",
            ["management:endpoints:actuator:exposure:include:1"] = "dbmigrations",
            ["management:endpoints:actuator:exposure:exclude:0"] = "trace",
            ["management:endpoints:actuator:exposure:exclude:1"] = "env"
        };

        var options = GetOptionsFromSettings<ManagementOptions>(appSettings);

        options.Exposure.Include.Should().HaveCount(2);
        options.Exposure.Include.Should().Contain("httptrace");
        options.Exposure.Include.Should().Contain("dbmigrations");

        options.Exposure.Exclude.Should().HaveCount(2);
        options.Exposure.Exclude.Should().Contain("trace");
        options.Exposure.Exclude.Should().Contain("env");
    }

    [Fact]
    public void ExposureBindsToSpringSettings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:web:exposure:include"] = "heapdump,env",
            ["management:endpoints:web:exposure:exclude"] = "dbmigrations,info"
        };

        var options = GetOptionsFromSettings<ManagementOptions>(appSettings);

        options.Exposure.Include.Should().HaveCount(2);
        options.Exposure.Include.Should().Contain("heapdump");
        options.Exposure.Include.Should().Contain("env");

        options.Exposure.Exclude.Should().HaveCount(2);
        options.Exposure.Exclude.Should().Contain("dbmigrations");
        options.Exposure.Exclude.Should().Contain("info");
    }

    [Fact]
    public void CombinesSteeltoeSettingsWithSpringSettings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:web:exposure:include"] = "heapdump, env",
            ["management:endpoints:web:exposure:exclude"] = "dbmigrations, info",
            ["management:endpoints:actuator:exposure:include:0"] = "httptrace",
            ["management:endpoints:actuator:exposure:include:1"] = "dbmigrations",
            ["management:endpoints:actuator:exposure:exclude:0"] = "trace",
            ["management:endpoints:actuator:exposure:exclude:1"] = "env"
        };

        var options = GetOptionsFromSettings<ManagementOptions>(appSettings);

        options.Exposure.Include.Should().HaveCount(4);
        options.Exposure.Include.Should().Contain("heapdump");
        options.Exposure.Include.Should().Contain("env");
        options.Exposure.Include.Should().Contain("httptrace");
        options.Exposure.Include.Should().Contain("dbmigrations");

        options.Exposure.Exclude.Should().HaveCount(4);
        options.Exposure.Exclude.Should().Contain("dbmigrations");
        options.Exposure.Exclude.Should().Contain("info");
        options.Exposure.Exclude.Should().Contain("trace");
        options.Exposure.Exclude.Should().Contain("env");
    }

    [Fact]
    public void ExposureDoesNotThrowOnInvalidSpringSettings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:web:exposure:include"] = ",,  ,heapdump;env"
        };

        var options = GetOptionsFromSettings<ManagementOptions>(appSettings);

        options.Exposure.Include.Should().HaveCount(1);
        options.Exposure.Include.Should().Contain("heapdump;env");

        options.Exposure.Exclude.Should().BeEmpty();
    }
}

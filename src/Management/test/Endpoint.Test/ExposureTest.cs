// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Options;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test;

public sealed class ExposureTest : BaseTest
{
    [Fact]
    public void ExposureReturnsDefaults()
    {
        var options = GetOptionsFromSettings<ManagementOptions>();

        Assert.NotNull(options.Exposure);
        Assert.Contains("health", options.Exposure.Include);
        Assert.Contains("info", options.Exposure.Include);
        Assert.Empty(options.Exposure.Exclude);
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

        Assert.NotNull(options.Exposure);
        Assert.Contains("httptrace", options.Exposure.Include);
        Assert.Contains("dbmigrations", options.Exposure.Include);
        Assert.Contains("trace", options.Exposure.Exclude);
        Assert.Contains("env", options.Exposure.Exclude);
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

        Assert.NotNull(options.Exposure);
        Assert.Contains("heapdump", options.Exposure.Include);
        Assert.Contains("env", options.Exposure.Include);
        Assert.Contains("dbmigrations", options.Exposure.Exclude);
        Assert.Contains("info", options.Exposure.Exclude);
    }

    [Fact]
    public void ExposureDoesNotThrowOnInvalidSpringSettings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:web:exposure:include"] = "heapdump;env"
        };

        var options = GetOptionsFromSettings<ManagementOptions>(appSettings);

        Assert.NotNull(options.Exposure);
        Assert.Contains("heapdump;env", options.Exposure.Include);
        Assert.Empty(options.Exposure.Exclude);
    }
}

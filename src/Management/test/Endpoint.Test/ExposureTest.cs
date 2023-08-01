// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test;

public sealed class ExposureTest
{
    [Fact]
    public void ExposureReturnsDefaults()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        var exposure = new Exposure(configurationRoot);

        Assert.Contains("health", exposure.Include);
        Assert.Contains("info", exposure.Include);
        Assert.Empty(exposure.Exclude);
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

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var exposure = new Exposure(configurationRoot);

        Assert.Contains("httptrace", exposure.Include);
        Assert.Contains("dbmigrations", exposure.Include);
        Assert.Contains("trace", exposure.Exclude);
        Assert.Contains("env", exposure.Exclude);
    }

    [Fact]
    public void ExposureBindsToSpringSettings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:web:exposure:include"] = "heapdump,env",
            ["management:endpoints:web:exposure:exclude"] = "dbmigrations,info"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var exposure = new Exposure(configurationRoot);

        Assert.Contains("heapdump", exposure.Include);
        Assert.Contains("env", exposure.Include);
        Assert.Contains("dbmigrations", exposure.Exclude);
        Assert.Contains("info", exposure.Exclude);
    }

    [Fact]
    public void ExposureDoesNotThrowOnInvalidSpringSettings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:web:exposure:include"] = "heapdump;env"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var exposure = new Exposure(configurationRoot);

        Assert.Contains("heapdump;env", exposure.Include);
        Assert.Empty(exposure.Exclude);
    }
}

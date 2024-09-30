// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.Health.Contributors;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health.Contributors;

public sealed class DiskSpaceContributorOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var options = new DiskSpaceContributorOptions();
        Assert.Equal(".", options.Path);
        Assert.Equal(10 * 1024 * 1024, options.Threshold);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:health:diskSpace:enabled"] = "true",
            ["management:endpoints:health:diskSpace:path"] = "foobar",
            ["management:endpoints:health:diskSpace:threshold"] = "5"
        };

        var options = GetOptionsFromSettings<DiskSpaceContributorOptions>(appSettings);

        Assert.True(options.Enabled);
        Assert.Equal("foobar", options.Path);
        Assert.Equal(5, options.Threshold);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Endpoint.Actuators.Info;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Hypermedia;

public sealed class HypermediaEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var options = GetOptionsFromSettings<HypermediaEndpointOptions>();
        Assert.Equal(string.Empty, options.Id);
        Assert.Equal(string.Empty, options.Path);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:info:path"] = "infopath"
        };

        InfoEndpointOptions options = GetOptionsFromSettings<InfoEndpointOptions, ConfigureInfoEndpointOptions>(appSettings);

        Assert.Equal("info", options.Id);
        Assert.Equal("infopath", options.Path);
    }

    [Fact]
    public void CanSetEmptyPathWithDifferentId()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Actuator:Id"] = "some",
            ["Management:Endpoints:Actuator:Path"] = string.Empty
        };

        HypermediaEndpointOptions options = GetOptionsFromSettings<HypermediaEndpointOptions, ConfigureHypermediaEndpointOptions>(appSettings);

        options.Id.Should().Be("some");
        options.Path.Should().BeEmpty();
    }
}

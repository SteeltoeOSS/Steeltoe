// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Actuators.HttpExchanges;

namespace Steeltoe.Management.Endpoint.Test.Actuators.HttpExchanges;

public sealed class HttpExchangesEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var options = GetOptionsFromSettings<HttpExchangesEndpointOptions>();
        options.Enabled.Should().BeNull();
        options.Id.Should().Be("httpexchanges");
        options.Capacity.Should().Be(100);
        options.IncludeRequestHeaders.Should().BeTrue();
        options.IncludeResponseHeaders.Should().BeTrue();
        options.IncludePathInfo.Should().BeTrue();
        options.IncludeQueryString.Should().BeTrue();
        options.IncludeUserPrincipal.Should().BeFalse();
        options.IncludeRemoteAddress.Should().BeFalse();
        options.IncludeSessionId.Should().BeFalse();
        options.IncludeTimeTaken.Should().BeTrue();
        options.Reverse.Should().BeTrue();
        options.RequestHeaders.Should().HaveCount(26);
        options.ResponseHeaders.Should().HaveCount(19);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appsettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:loggers:enabled"] = "false",
            ["management:endpoints:httpExchanges:enabled"] = "true",
            ["management:endpoints:httpExchanges:capacity"] = "1000",
            ["management:endpoints:httpExchanges:includeTimeTaken"] = "false",
            ["management:endpoints:httpExchanges:includeRequestHeaders"] = "false",
            ["management:endpoints:httpExchanges:includeResponseHeaders"] = "false",
            ["management:endpoints:httpExchanges:includePathInfo"] = "false",
            ["management:endpoints:httpExchanges:includeUserPrincipal"] = "true",
            ["management:endpoints:httpExchanges:includeQueryString"] = "true",
            ["management:endpoints:httpExchanges:includeRemoteAddress"] = "true",
            ["management:endpoints:httpExchanges:includeSessionId"] = "true",
            ["management:endpoints:httpExchanges:reverse"] = "false",
            ["management:endpoints:cloudfoundry:enabled"] = "true"
        };

        var endpointOptions = GetOptionsFromSettings<HttpExchangesEndpointOptions>(appsettings);
        var cloudFoundryEndpointOptions = GetOptionsFromSettings<CloudFoundryEndpointOptions>(appsettings);

        cloudFoundryEndpointOptions.Enabled.Should().BeTrue();
        cloudFoundryEndpointOptions.Id.Should().Be(string.Empty);
        cloudFoundryEndpointOptions.Path.Should().Be(string.Empty);
        cloudFoundryEndpointOptions.ValidateCertificates.Should().BeTrue();

        endpointOptions.Enabled.Should().BeTrue();
        endpointOptions.Id.Should().Be("httpexchanges");
        endpointOptions.Path.Should().Be("httpexchanges");
        endpointOptions.Capacity.Should().Be(1000);
        endpointOptions.IncludeTimeTaken.Should().BeFalse();
        endpointOptions.IncludeRequestHeaders.Should().BeFalse();
        endpointOptions.IncludeResponseHeaders.Should().BeFalse();
        endpointOptions.IncludePathInfo.Should().BeFalse();
        endpointOptions.IncludeUserPrincipal.Should().BeTrue();
        endpointOptions.IncludeQueryString.Should().BeTrue();
        endpointOptions.IncludeRemoteAddress.Should().BeTrue();
        endpointOptions.IncludeSessionId.Should().BeTrue();
        endpointOptions.Reverse.Should().BeFalse();
    }
}

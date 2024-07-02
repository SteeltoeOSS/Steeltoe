// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Trace;

namespace Steeltoe.Management.Endpoint.Test.Trace;

public sealed class TraceEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var options = GetOptionsFromSettings<TraceEndpointOptions>();
        Assert.Null(options.Enabled);
        Assert.Equal("httptrace", options.Id);
        Assert.Equal(100, options.Capacity);
        Assert.True(options.AddTimeTaken);
        Assert.True(options.AddRequestHeaders);
        Assert.True(options.AddResponseHeaders);
        Assert.False(options.AddPathInfo);
        Assert.False(options.AddUserPrincipal);
        Assert.False(options.AddParameters);
        Assert.False(options.AddQueryString);
        Assert.False(options.AddAuthType);
        Assert.False(options.AddRemoteAddress);
        Assert.False(options.AddSessionId);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appsettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:loggers:enabled"] = "false",
            ["management:endpoints:httptrace:enabled"] = "true",
            ["management:endpoints:httptrace:capacity"] = "1000",
            ["management:endpoints:httptrace:addTimeTaken"] = "false",
            ["management:endpoints:httptrace:addRequestHeaders"] = "false",
            ["management:endpoints:httptrace:addResponseHeaders"] = "false",
            ["management:endpoints:httptrace:addPathInfo"] = "true",
            ["management:endpoints:httptrace:addUserPrincipal"] = "true",
            ["management:endpoints:httptrace:addParameters"] = "true",
            ["management:endpoints:httptrace:addQueryString"] = "true",
            ["management:endpoints:httptrace:addAuthType"] = "true",
            ["management:endpoints:httptrace:addRemoteAddress"] = "true",
            ["management:endpoints:httptrace:addSessionId"] = "true",
            ["management:endpoints:cloudfoundry:validatecertificates"] = "true",
            ["management:endpoints:cloudfoundry:enabled"] = "true"
        };

        TraceEndpointOptions endpointOptions =
            GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>(appsettings).Get(string.Empty);

        CloudFoundryEndpointOptions cloudFoundryEndpointOptions =
            GetOptionsFromSettings<CloudFoundryEndpointOptions, ConfigureCloudFoundryEndpointOptions>(appsettings);

        Assert.True(cloudFoundryEndpointOptions.Enabled);
        Assert.Equal(string.Empty, cloudFoundryEndpointOptions.Id);
        Assert.Equal(string.Empty, cloudFoundryEndpointOptions.Path);
        Assert.True(cloudFoundryEndpointOptions.ValidateCertificates);

        Assert.True(endpointOptions.Enabled);
        Assert.Equal("httptrace", endpointOptions.Id);
        Assert.Equal("httptrace", endpointOptions.Path);
        Assert.Equal(1000, endpointOptions.Capacity);
        Assert.False(endpointOptions.AddTimeTaken);
        Assert.False(endpointOptions.AddRequestHeaders);
        Assert.False(endpointOptions.AddResponseHeaders);
        Assert.True(endpointOptions.AddPathInfo);
        Assert.True(endpointOptions.AddUserPrincipal);
        Assert.True(endpointOptions.AddParameters);
        Assert.True(endpointOptions.AddQueryString);
        Assert.True(endpointOptions.AddAuthType);
        Assert.True(endpointOptions.AddRemoteAddress);
        Assert.True(endpointOptions.AddSessionId);
    }
}

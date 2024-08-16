// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Actuators.Trace;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Trace;

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
    public void Constructor_BindsConfigurationCorrectly_V1()
    {
        var appsettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:loggers:enabled"] = "false",
            ["management:endpoints:trace:enabled"] = "true",
            ["management:endpoints:trace:capacity"] = "1000",
            ["management:endpoints:trace:addTimeTaken"] = "false",
            ["management:endpoints:trace:addRequestHeaders"] = "false",
            ["management:endpoints:trace:addResponseHeaders"] = "false",
            ["management:endpoints:trace:addPathInfo"] = "true",
            ["management:endpoints:trace:addUserPrincipal"] = "true",
            ["management:endpoints:trace:addParameters"] = "true",
            ["management:endpoints:trace:addQueryString"] = "true",
            ["management:endpoints:trace:addAuthType"] = "true",
            ["management:endpoints:trace:addRemoteAddress"] = "true",
            ["management:endpoints:trace:addSessionId"] = "true",
            ["management:endpoints:cloudfoundry:validateCertificates"] = "true",
            ["management:endpoints:cloudfoundry:enabled"] = "true"
        };

        TraceEndpointOptions endpointOptions = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>(appsettings).Get("V1");

        CloudFoundryEndpointOptions cloudFoundryEndpointOptions =
            GetOptionsFromSettings<CloudFoundryEndpointOptions, ConfigureCloudFoundryEndpointOptions>(appsettings);

        Assert.True(cloudFoundryEndpointOptions.Enabled);
        Assert.Equal(string.Empty, cloudFoundryEndpointOptions.Id);
        Assert.Equal(string.Empty, cloudFoundryEndpointOptions.Path);
        Assert.True(cloudFoundryEndpointOptions.ValidateCertificates);

        Assert.True(endpointOptions.Enabled);
        Assert.Equal("trace", endpointOptions.Id);
        Assert.Equal("trace", endpointOptions.Path);
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

    [Fact]
    public void Constructor_BindsConfigurationCorrectly_V2()
    {
        var appsettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:loggers:enabled"] = "false",
            ["management:endpoints:httpTrace:enabled"] = "true",
            ["management:endpoints:httpTrace:capacity"] = "1000",
            ["management:endpoints:httpTrace:addTimeTaken"] = "false",
            ["management:endpoints:httpTrace:addRequestHeaders"] = "false",
            ["management:endpoints:httpTrace:addResponseHeaders"] = "false",
            ["management:endpoints:httpTrace:addPathInfo"] = "true",
            ["management:endpoints:httpTrace:addUserPrincipal"] = "true",
            ["management:endpoints:httpTrace:addParameters"] = "true",
            ["management:endpoints:httpTrace:addQueryString"] = "true",
            ["management:endpoints:httpTrace:addAuthType"] = "true",
            ["management:endpoints:httpTrace:addRemoteAddress"] = "true",
            ["management:endpoints:httpTrace:addSessionId"] = "true",
            ["management:endpoints:cloudfoundry:validateCertificates"] = "true",
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

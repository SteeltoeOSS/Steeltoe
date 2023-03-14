// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Trace;
using Xunit;
namespace Steeltoe.Management.Endpoint.Test.Trace;

public class TraceEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var opts = GetOptionsFromSettings<TraceEndpointOptions>();
        Assert.Null(opts.Enabled);
        Assert.Equal("httptrace", opts.Id);
        Assert.Equal(100, opts.Capacity);
        Assert.True(opts.AddTimeTaken);
        Assert.True(opts.AddRequestHeaders);
        Assert.True(opts.AddResponseHeaders);
        Assert.False(opts.AddPathInfo);
        Assert.False(opts.AddUserPrincipal);
        Assert.False(opts.AddParameters);
        Assert.False(opts.AddQueryString);
        Assert.False(opts.AddAuthType);
        Assert.False(opts.AddRemoteAddress);
        Assert.False(opts.AddSessionId);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appsettings = new Dictionary<string, string>
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

        var opts = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>(appsettings).Get(string.Empty);
        var cloudOpts = GetOptionsFromSettings<CloudFoundryEndpointOptions, ConfigureCloudFoundryEndpointOptions>(appsettings);

        Assert.True(cloudOpts.Enabled);
        Assert.Equal(string.Empty, cloudOpts.Id);
        Assert.Equal(string.Empty, cloudOpts.Path);
        Assert.True(cloudOpts.ValidateCertificates);

        Assert.True(opts.Enabled);
        Assert.Equal("httptrace", opts.Id);
        Assert.Equal("httptrace", opts.Path);
        Assert.Equal(1000, opts.Capacity);
        Assert.False(opts.AddTimeTaken);
        Assert.False(opts.AddRequestHeaders);
        Assert.False(opts.AddResponseHeaders);
        Assert.True(opts.AddPathInfo);
        Assert.True(opts.AddUserPrincipal);
        Assert.True(opts.AddParameters);
        Assert.True(opts.AddQueryString);
        Assert.True(opts.AddAuthType);
        Assert.True(opts.AddRemoteAddress);
        Assert.True(opts.AddSessionId);
    }
}

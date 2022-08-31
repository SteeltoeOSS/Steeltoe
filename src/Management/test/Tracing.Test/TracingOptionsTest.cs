// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Xunit;

namespace Steeltoe.Management.Tracing.Test;

public class TracingOptionsTest
{
    [Fact]
    public void InitializedWithDefaults()
    {
        IConfiguration configuration = TestHelpers.GetConfigurationFromDictionary(new Dictionary<string, string>());
        var opts = new TracingOptions(new ApplicationInstanceInfo(configuration), configuration);

        Assert.Equal(Assembly.GetEntryAssembly().GetName().Name, opts.Name);
        Assert.Equal(TracingOptions.DefaultIngressIgnorePattern, opts.IngressIgnorePattern);
        Assert.False(opts.AlwaysSample);
        Assert.False(opts.NeverSample);
        Assert.False(opts.UseShortTraceIds);
        Assert.Equal(TracingOptions.DefaultEgressIgnorePattern, opts.EgressIgnorePattern);
    }

    [Fact]
    public void ThrowsIfConfigNull()
    {
        const IConfiguration configuration = null;
        Assert.Throws<ArgumentNullException>(() => new TracingOptions(null, configuration));
    }

    [Fact]
    public void BindsConfigurationCorrectly()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["management:tracing:name"] = "foobar",
            ["management:tracing:ingressIgnorePattern"] = "pattern",
            ["management:tracing:egressIgnorePattern"] = "pattern",
            ["management:tracing:alwaysSample"] = "true",
            ["management:tracing:neverSample"] = "true",
            ["management:tracing:useShortTraceIds"] = "true"
        };

        IConfiguration configuration = TestHelpers.GetConfigurationFromDictionary(appsettings);
        var opts = new TracingOptions(new ApplicationInstanceInfo(configuration), configuration);

        Assert.Equal("foobar", opts.Name);
        Assert.Equal("pattern", opts.IngressIgnorePattern);
        Assert.True(opts.AlwaysSample);
        Assert.True(opts.NeverSample);
        Assert.True(opts.UseShortTraceIds);
        Assert.Equal("pattern", opts.EgressIgnorePattern);
    }

    [Fact]
    public void ApplicationName_ReturnsExpected()
    {
        var appsettings = new Dictionary<string, string>();
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = builder.Build();
        var appInstanceInfo = new ApplicationInstanceInfo(configurationRoot);

        // Uses Assembly name as default
        var opts = new TracingOptions(appInstanceInfo, configurationRoot);
        Assert.Equal(Assembly.GetEntryAssembly().GetName().Name, opts.Name);

        // Finds Spring app name
        appsettings.Add("spring:application:name", "SpringApplicationName");
        configurationRoot = builder.Build();
        appInstanceInfo = new ApplicationInstanceInfo(configurationRoot);
        opts = new TracingOptions(appInstanceInfo, configurationRoot);
        Assert.Equal("SpringApplicationName", opts.Name);

        // Platform app name overrides spring name
        appsettings.Add("application:name", "PlatformName");
        configurationRoot = builder.Build();
        appInstanceInfo = new ApplicationInstanceInfo(configurationRoot);
        opts = new TracingOptions(appInstanceInfo, configurationRoot);
        Assert.Equal("PlatformName", opts.Name);

        // Finds and uses management name
        appsettings.Add("management:name", "ManagementName");
        configurationRoot = builder.Build();
        appInstanceInfo = new ApplicationInstanceInfo(configurationRoot);
        opts = new TracingOptions(appInstanceInfo, configurationRoot);
        Assert.Equal("ManagementName", opts.Name);

        // management:tracing name beats all else
        appsettings.Add("management:tracing:name", "ManagementTracingName");
        configurationRoot = builder.Build();
        appInstanceInfo = new ApplicationInstanceInfo(configurationRoot);
        opts = new TracingOptions(appInstanceInfo, configurationRoot);
        Assert.Equal("ManagementTracingName", opts.Name);
    }
}

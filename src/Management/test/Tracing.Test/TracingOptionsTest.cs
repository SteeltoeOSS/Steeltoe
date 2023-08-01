// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Management.Tracing.Test;

public sealed class TracingOptionsTest
{
    [Fact]
    public void InitializedWithDefaults()
    {
        IConfiguration configuration = TestHelpers.GetConfigurationFromDictionary(new Dictionary<string, string>());
        var options = new TracingOptions(new ApplicationInstanceInfo(configuration), configuration);

        Assert.Equal(Assembly.GetEntryAssembly()!.GetName().Name, options.Name);
        Assert.Equal(TracingOptions.DefaultIngressIgnorePattern, options.IngressIgnorePattern);
        Assert.False(options.AlwaysSample);
        Assert.False(options.NeverSample);
        Assert.False(options.UseShortTraceIds);
        Assert.Equal(TracingOptions.DefaultEgressIgnorePattern, options.EgressIgnorePattern);
    }

    [Fact]
    public void ThrowsIfConfigNull()
    {
        const IConfiguration? configuration = null;
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
        var options = new TracingOptions(new ApplicationInstanceInfo(configuration), configuration);

        Assert.Equal("foobar", options.Name);
        Assert.Equal("pattern", options.IngressIgnorePattern);
        Assert.True(options.AlwaysSample);
        Assert.True(options.NeverSample);
        Assert.True(options.UseShortTraceIds);
        Assert.Equal("pattern", options.EgressIgnorePattern);
    }

    [Fact]
    public void ApplicationName_ReturnsExpected()
    {
        var appsettings = new Dictionary<string, string?>();
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = builder.Build();
        var appInstanceInfo = new ApplicationInstanceInfo(configurationRoot);

        // Uses Assembly name as default
        var options = new TracingOptions(appInstanceInfo, configurationRoot);
        Assert.Equal(Assembly.GetEntryAssembly()!.GetName().Name, options.Name);

        // Finds Spring app name
        appsettings.Add("spring:application:name", "SpringApplicationName");
        configurationRoot = builder.Build();
        appInstanceInfo = new ApplicationInstanceInfo(configurationRoot);
        options = new TracingOptions(appInstanceInfo, configurationRoot);
        Assert.Equal("SpringApplicationName", options.Name);

        // Platform app name overrides spring name
        appsettings.Add("application:name", "PlatformName");
        configurationRoot = builder.Build();
        appInstanceInfo = new ApplicationInstanceInfo(configurationRoot);
        options = new TracingOptions(appInstanceInfo, configurationRoot);
        Assert.Equal("PlatformName", options.Name);

        // Finds and uses management name
        appsettings.Add("management:name", "ManagementName");
        configurationRoot = builder.Build();
        appInstanceInfo = new ApplicationInstanceInfo(configurationRoot);
        options = new TracingOptions(appInstanceInfo, configurationRoot);
        Assert.Equal("ManagementName", options.Name);

        // management:tracing name beats all else
        appsettings.Add("management:tracing:name", "ManagementTracingName");
        configurationRoot = builder.Build();
        appInstanceInfo = new ApplicationInstanceInfo(configurationRoot);
        options = new TracingOptions(appInstanceInfo, configurationRoot);
        Assert.Equal("ManagementTracingName", options.Name);
    }
}

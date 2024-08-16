// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;
using Steeltoe.Common.Extensions;

namespace Steeltoe.Management.Tracing.Test;

public sealed class TracingOptionsTest
{
    [Fact]
    public void InitializedWithDefaults()
    {
        var options = new TracingOptions();

        options.Name.Should().BeNull();
        options.IngressIgnorePattern.Should().BeNull();
        options.EgressIgnorePattern.Should().BeNull();
        options.MaxPayloadSizeInBytes.Should().Be(4096);
        options.AlwaysSample.Should().BeFalse();
        options.NeverSample.Should().BeFalse();
        options.UseShortTraceIds.Should().BeFalse();
        options.PropagationType.Should().Be("B3");
        options.SingleB3Header.Should().BeTrue();
        options.ExporterEndpoint.Should().BeNull();
    }

    [Fact]
    public void BindsConfigurationCorrectly()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:tracing:name"] = "foobar",
            ["management:tracing:ingressIgnorePattern"] = "pattern-in",
            ["management:tracing:egressIgnorePattern"] = "pattern-out",
            ["management:tracing:alwaysSample"] = "true",
            ["management:tracing:neverSample"] = "true",
            ["management:tracing:useShortTraceIds"] = "true"
        };

        TracingOptions options = ConfigureTracingOptions(appSettings);

        options.Name.Should().Be("foobar");
        options.IngressIgnorePattern.Should().Be("pattern-in");
        options.EgressIgnorePattern.Should().Be("pattern-out");
        options.MaxPayloadSizeInBytes.Should().Be(4096);
        options.AlwaysSample.Should().BeTrue();
        options.NeverSample.Should().BeTrue();
        options.UseShortTraceIds.Should().BeTrue();
        options.PropagationType.Should().Be("B3");
        options.SingleB3Header.Should().BeTrue();
        options.ExporterEndpoint.Should().BeNull();
    }

    [Fact]
    public void ApplicationName_ReturnsExpected()
    {
        var appSettings = new Dictionary<string, string?>();

        // Uses Assembly name as default
        TracingOptions options = ConfigureTracingOptions(appSettings);
        options.Name.Should().Be(Assembly.GetEntryAssembly()!.GetName().Name);

        // Finds Spring app name
        appSettings.Add("spring:application:name", "SpringApplicationName");
        options = ConfigureTracingOptions(appSettings);
        options.Name.Should().Be("SpringApplicationName");

        // management:tracing:name beats all else
        appSettings.Add("management:tracing:name", "ManagementTracingName");
        options = ConfigureTracingOptions(appSettings);
        options.Name.Should().Be("ManagementTracingName");
    }

    private TracingOptions ConfigureTracingOptions(IDictionary<string, string?> appSettings)
    {
        var options = new TracingOptions();

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = builder.Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddApplicationInstanceInfo();
        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var appInfo = serviceProvider.GetRequiredService<IApplicationInstanceInfo>();
        var configurer = new ConfigureTracingOptions(configuration, appInfo);
        configurer.Configure(options);

        return options;
    }
}

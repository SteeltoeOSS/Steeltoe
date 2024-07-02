// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace Steeltoe.Logging.DynamicSerilog.Test;

public sealed class SerilogConfigurationExtensionsTest
{
    [Fact]
    public void SerilogOptions_Set_Correctly()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "Serilog:MinimumLevel:Default", "Error" },
            { "Serilog:MinimumLevel:Override:Microsoft", "Warning" },
            { "Serilog:MinimumLevel:Override:Steeltoe.Logging", "Verbose" },
            { "Serilog:MinimumLevel:Override:Steeltoe", "Information" }
        };

        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        var serilogOptions = new SerilogOptions();

        serilogOptions.SetSerilogOptions(configuration);

        serilogOptions.MinimumLevel.Should().NotBeNull();
        serilogOptions.MinimumLevel!.Default.Should().Be(LogEventLevel.Error);
        serilogOptions.MinimumLevel.Override["Microsoft"].Should().Be(LogEventLevel.Warning);
        serilogOptions.MinimumLevel.Override["Steeltoe"].Should().Be(LogEventLevel.Information);
        serilogOptions.MinimumLevel.Override["Steeltoe.Logging"].Should().Be(LogEventLevel.Verbose);
        serilogOptions.GetSerilogConfiguration().Should().NotBeNull();
    }

    [Fact]
    public void SerilogOptions_Set_Correctly_When_MinimumLevel_Is_String()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "Serilog:MinimumLevel", "Error" }
        };

        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        var serilogOptions = new SerilogOptions();

        serilogOptions.SetSerilogOptions(configuration);

        serilogOptions.MinimumLevel.Should().NotBeNull();
        serilogOptions.MinimumLevel!.Default.Should().Be(LogEventLevel.Error);
        serilogOptions.GetSerilogConfiguration().Should().NotBeNull();
    }

    [Fact]
    public void SerilogOptions_Set_Correctly_Via_LoggerConfiguration()
    {
        LoggerConfiguration loggerConfiguration = new LoggerConfiguration().MinimumLevel.Debug().MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Steeltoe.Logging", LogEventLevel.Verbose).MinimumLevel.Override("Steeltoe", LogEventLevel.Information);

        var serilogOptions = new SerilogOptions();
        serilogOptions.SetSerilogOptions(loggerConfiguration);

        serilogOptions.MinimumLevel.Should().NotBeNull();
        serilogOptions.MinimumLevel!.Default.Should().Be(LogEventLevel.Debug);
        serilogOptions.MinimumLevel.Override["Microsoft"].Should().Be(LogEventLevel.Warning);
        serilogOptions.MinimumLevel.Override["Steeltoe"].Should().Be(LogEventLevel.Information);
        serilogOptions.MinimumLevel.Override["Steeltoe.Logging"].Should().Be(LogEventLevel.Verbose);
        serilogOptions.GetSerilogConfiguration().Should().NotBeNull();
    }

    [Fact]
    public void SerilogOptions_NoError_When_NotConfigured()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        var serilogOptions = new SerilogOptions();

        serilogOptions.SetSerilogOptions(configuration);

        serilogOptions.MinimumLevel.Should().NotBeNull();
        serilogOptions.MinimumLevel!.Default.Should().Be(LogEventLevel.Information);
        serilogOptions.GetSerilogConfiguration().Should().NotBeNull();
    }
}

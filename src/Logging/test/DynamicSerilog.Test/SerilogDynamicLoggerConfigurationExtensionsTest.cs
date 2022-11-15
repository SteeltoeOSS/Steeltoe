// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Xunit;

namespace Steeltoe.Logging.DynamicSerilog.Test;

public class SerilogDynamicLoggerConfigurationExtensionsTest
{
    [Fact]
    public void SerilogOptions_Set_Correctly()
    {
        var appSettings = new Dictionary<string, string>
        {
            { "Serilog:MinimumLevel:Default", "Error" },
            { "Serilog:MinimumLevel:Override:Microsoft", "Warning" },
            { "Serilog:MinimumLevel:Override:Steeltoe.Logging", "Verbose" },
            { "Serilog:MinimumLevel:Override:Steeltoe", "Information" }
        };

        IConfigurationBuilder builder = new ConfigurationBuilder().AddInMemoryCollection(appSettings);

        var serilogOptions = new SerilogOptions();
        serilogOptions.SetSerilogOptions(builder.Build());

        Assert.Equal(LogEventLevel.Error, serilogOptions.MinimumLevel.Default);
        Assert.Equal(LogEventLevel.Warning, serilogOptions.MinimumLevel.Override["Microsoft"]);
        Assert.Equal(LogEventLevel.Information, serilogOptions.MinimumLevel.Override["Steeltoe"]);
        Assert.Equal(LogEventLevel.Verbose, serilogOptions.MinimumLevel.Override["Steeltoe.Logging"]);
        Assert.NotNull(serilogOptions.GetSerilogConfiguration());
    }

    [Fact]
    public void SerilogOptions_Set_Correctly_When_MinimumLevel_Is_String()
    {
        var appSettings = new Dictionary<string, string>
        {
            { "Serilog:MinimumLevel", "Error" }
        };

        IConfigurationBuilder builder = new ConfigurationBuilder().AddInMemoryCollection(appSettings);

        var serilogOptions = new SerilogOptions();
        serilogOptions.SetSerilogOptions(builder.Build());

        Assert.Equal(LogEventLevel.Error, serilogOptions.MinimumLevel.Default);
        Assert.NotNull(serilogOptions.GetSerilogConfiguration());
    }

    [Fact]
    public void SerilogOptions_Set_Correctly_Via_LoggerConfiguration()
    {
        LoggerConfiguration loggerConfiguration = new LoggerConfiguration().MinimumLevel.Debug().MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Steeltoe.Logging", LogEventLevel.Verbose).MinimumLevel.Override("Steeltoe", LogEventLevel.Information);

        var serilogOptions = new SerilogOptions();
        serilogOptions.SetSerilogOptions(loggerConfiguration);

        Assert.Equal(LogEventLevel.Debug, serilogOptions.MinimumLevel.Default);
        Assert.Equal(LogEventLevel.Warning, serilogOptions.MinimumLevel.Override["Microsoft"]);
        Assert.Equal(LogEventLevel.Verbose, serilogOptions.MinimumLevel.Override["Steeltoe.Logging"]);
        Assert.NotNull(serilogOptions.GetSerilogConfiguration());
    }

    [Fact]
    public void SerilogOptions_NoError_When_NotConfigured()
    {
        var appSettings = new Dictionary<string, string>();
        IConfigurationBuilder builder = new ConfigurationBuilder().AddInMemoryCollection(appSettings);

        var serilogOptions = new SerilogOptions();
        serilogOptions.SetSerilogOptions(builder.Build());

        Assert.Equal(LogEventLevel.Information, serilogOptions.MinimumLevel.Default);
        Assert.NotNull(serilogOptions.GetSerilogConfiguration());
    }
}

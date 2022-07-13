// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Xunit;

namespace Steeltoe.Extensions.Logging.Test;

public partial class DynamicLoggingBuilderTest
{
    [Fact]
    public void DynamicLevelSetting_ParameterlessAddDynamic_AddsConsoleOptions()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();
        var services = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.AddConfiguration(configuration.GetSection("Logging"));
                builder.AddDynamicConsole();
            })
            .BuildServiceProvider();

        var options = services.GetService<IOptionsMonitor<ConsoleLoggerOptions>>();

        Assert.NotNull(options);
        Assert.NotNull(options.CurrentValue);
    }

    [Fact]
    public void AddDynamicConsole_DoesNotSetColorLocal()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()).Build();
        var services = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.AddConfiguration(configuration.GetSection("Logging"));
                builder.AddDynamicConsole();
            }).BuildServiceProvider();

        var options = services.GetService(typeof(IOptions<ConsoleLoggerOptions>)) as IOptions<ConsoleLoggerOptions>;

        Assert.NotNull(options);
    }

    [Fact]
    public void AddDynamicConsole_DisablesColorOnPivotalPlatform()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", "not empty");
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()).Build();
        var services = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.AddConfiguration(configuration.GetSection("Logging"));
                builder.AddDynamicConsole();
            }).BuildServiceProvider();

        var options = services.GetService(typeof(IOptions<ConsoleLoggerOptions>)) as IOptions<ConsoleLoggerOptions>;

        Assert.NotNull(options);
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", string.Empty);
    }
}
#endif

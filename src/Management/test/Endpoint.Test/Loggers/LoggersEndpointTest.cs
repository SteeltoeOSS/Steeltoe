// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Logging;
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Loggers;

public class LoggersEndpointTest : BaseTest
{
    private readonly ITestOutputHelper _output;

    public LoggersEndpointTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void AddLevels_AddsExpected()
    {
        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddLogging(builder => builder.AddDynamicConsole());
            services.AddLoggersActuatorServices();
        };

        var ep = (LoggersEndpoint)tc.GetService<ILoggersEndpoint>();

        var dict = new Dictionary<string, object>();
        ep.AddLevels(dict);

        Assert.Single(dict);
        Assert.True(dict.ContainsKey("levels"));
        var levels = dict["levels"] as List<string>;
        Assert.NotNull(levels);
        Assert.Equal(7, levels.Count);

        Assert.Contains("OFF", levels);
        Assert.Contains("FATAL", levels);
        Assert.Contains("ERROR", levels);
        Assert.Contains("WARN", levels);
        Assert.Contains("INFO", levels);
        Assert.Contains("DEBUG", levels);
        Assert.Contains("TRACE", levels);
    }

    [Fact]
    public void SetLogLevel_ThrowsIfNullName()
    {
        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddLogging(builder => builder.AddDynamicConsole());
            services.AddLoggersActuatorServices();
        };

        var ep = (LoggersEndpoint)tc.GetService<ILoggersEndpoint>();

        Assert.Throws<ArgumentNullException>(() => ep.SetLogLevel(new TestLogProvider(), null, null));
    }

    [Fact]
    public void SetLogLevel_CallsProvider()
    {
        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddLogging(builder => builder.AddDynamicConsole());
            services.AddLoggersActuatorServices();
        };

        var ep = (LoggersEndpoint)tc.GetService<ILoggersEndpoint>();
        var provider = new TestLogProvider();
        ep.SetLogLevel(provider, "foobar", "WARN");

        Assert.Equal("foobar", provider.Category);
        Assert.Equal(LogLevel.Warning, provider.Level);
    }

    [Fact]
    public void GetLoggerConfigurations_NullProvider()
    {
        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddLogging(builder => builder.AddDynamicConsole());
            services.AddLoggersActuatorServices();
        };

        var ep = (LoggersEndpoint)tc.GetService<ILoggersEndpoint>();
        ICollection<ILoggerConfiguration> result = ep.GetLoggerConfigurations(null);
        Assert.NotNull(result);
    }

    [Fact]
    public void GetLoggerConfiguration_CallsProvider()
    {
        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddLogging(builder => builder.AddDynamicConsole());
            services.AddLoggersActuatorServices();
        };

        var ep = (LoggersEndpoint)tc.GetService<ILoggersEndpoint>();
        var provider = new TestLogProvider();
        ICollection<ILoggerConfiguration> result = ep.GetLoggerConfigurations(provider);
        Assert.NotNull(result);
        Assert.True(provider.GetLoggerConfigurationsCalled);
    }

    [Fact]
    public async Task DoInvoke_NoChangeRequest_ReturnsExpected()
    {
        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddLogging(builder => builder.AddDynamicConsole());
            services.AddLoggersActuatorServices();
        };

        var ep = tc.GetService<ILoggersEndpoint>();

        Dictionary<string, object> result = await ep.InvokeAsync(null, CancellationToken.None);
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("levels"));
        var levels = result["levels"] as List<string>;
        Assert.NotNull(levels);
        Assert.Equal(7, levels.Count);

        Assert.True(result.ContainsKey("loggers"));
        var loggers = result["loggers"] as Dictionary<string, LoggerLevels>;
        Assert.NotNull(loggers);
    }
}

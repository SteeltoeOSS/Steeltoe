// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Logging;
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Management.Endpoint.Actuators.Loggers;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Loggers;

public sealed class LoggersEndpointTest : BaseTest
{
    private readonly ITestOutputHelper _output;

    public LoggersEndpointTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task GetLoggerConfiguration_CallsProvider()
    {
        using var testContext = new TestContext(_output);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IDynamicLoggerProvider, TestLogProvider>();
            services.AddLogging(builder => builder.AddDynamicConsole());
            services.AddLoggersActuator();
        };

        var handler = testContext.GetRequiredService<ILoggersEndpointHandler>();
        var provider = (TestLogProvider)testContext.GetRequiredService<IDynamicLoggerProvider>();

        _ = await handler.InvokeAsync(new LoggersRequest(), CancellationToken.None);

        provider.GetLoggerConfigurationsCalled.Should().BeTrue();
    }

    [Fact]
    public async Task GetLoggerConfiguration_ReturnsExpected()
    {
        using var testContext = new TestContext(_output);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddLogging(builder => builder.AddDynamicConsole());
            services.AddLoggersActuator();
        };

        var handler = testContext.GetRequiredService<ILoggersEndpointHandler>();

        LoggersResponse response = await handler.InvokeAsync(new LoggersRequest(), CancellationToken.None);

        response.Should().NotBeNull();
        response.HasError.Should().BeFalse();
        response.Data.Should().NotBeNull();

        var levels = response.Data.Should().ContainKey("levels").WhoseValue.As<ICollection<string>>();
        levels.Should().HaveCount(7);
        levels.Should().Contain("OFF");
        levels.Should().Contain("FATAL");
        levels.Should().Contain("ERROR");
        levels.Should().Contain("WARN");
        levels.Should().Contain("INFO");
        levels.Should().Contain("DEBUG");
        levels.Should().Contain("TRACE");

        response.Data.Should().ContainKey("loggers").WhoseValue.As<IDictionary<string, LoggerLevels>>().Should().NotBeEmpty();
    }

    [Fact]
    public async Task SetLogLevel_CallsProvider()
    {
        using var testContext = new TestContext(_output);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IDynamicLoggerProvider, TestLogProvider>();
            services.AddLogging(builder => builder.AddDynamicConsole());
            services.AddLoggersActuator();
        };

        var handler = testContext.GetRequiredService<ILoggersEndpointHandler>();
        var provider = (TestLogProvider)testContext.GetRequiredService<IDynamicLoggerProvider>();

        var changeRequest = new LoggersRequest("foobar", "WARN");
        LoggersResponse response = await handler.InvokeAsync(changeRequest, CancellationToken.None);

        response.HasError.Should().BeFalse();
        response.Data.Should().BeEmpty();

        provider.Category.Should().Be("foobar");
        provider.MinLevel.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public async Task SetLogLevel_ReturnsExpected()
    {
        using var testContext = new TestContext(_output);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddLogging(builder => builder.AddDynamicConsole());
            services.AddLoggersActuator();
        };

        var handler = testContext.GetRequiredService<ILoggersEndpointHandler>();

        var changeRequest = new LoggersRequest("foobar", "WARN");
        LoggersResponse response = await handler.InvokeAsync(changeRequest, CancellationToken.None);

        response.HasError.Should().BeFalse();
        response.Data.Should().BeEmpty();
    }
}

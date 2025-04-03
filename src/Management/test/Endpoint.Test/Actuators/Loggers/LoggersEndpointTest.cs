// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Logging;
using Steeltoe.Management.Endpoint.Actuators.Loggers;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Loggers;

public sealed class LoggersEndpointTest(ITestOutputHelper testOutputHelper) : BaseTest
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    [Fact]
    public async Task GetLoggerConfiguration_CallsProvider()
    {
        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IDynamicLoggerProvider, TestLoggerProvider>();
            services.AddLoggersActuator();
        };

        var handler = testContext.GetRequiredService<ILoggersEndpointHandler>();
        var provider = (TestLoggerProvider)testContext.GetRequiredService<IDynamicLoggerProvider>();

        _ = await handler.InvokeAsync(new LoggersRequest(), TestContext.Current.CancellationToken);

        provider.HasCalledGetLogLevels.Should().BeTrue();
    }

    [Fact]
    public async Task GetLoggerConfiguration_ReturnsExpected()
    {
        using var testContext = new SteeltoeTestContext(_testOutputHelper);
        testContext.AdditionalServices = (services, _) => services.AddLoggersActuator();

        var handler = testContext.GetRequiredService<ILoggersEndpointHandler>();

        LoggersResponse? response = await handler.InvokeAsync(new LoggersRequest(), TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response.HasError.Should().BeFalse();

        response.Groups.Should().BeEmpty();

        response.Levels.Should().HaveCount(7);
        response.Levels.Should().Contain("OFF");
        response.Levels.Should().Contain("FATAL");
        response.Levels.Should().Contain("ERROR");
        response.Levels.Should().Contain("WARN");
        response.Levels.Should().Contain("INFO");
        response.Levels.Should().Contain("DEBUG");
        response.Levels.Should().Contain("TRACE");

        response.Loggers.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SetLogLevel_CallsProvider()
    {
        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IDynamicLoggerProvider, TestLoggerProvider>();
            services.AddLoggersActuator();
        };

        var handler = testContext.GetRequiredService<ILoggersEndpointHandler>();
        var provider = (TestLoggerProvider)testContext.GetRequiredService<IDynamicLoggerProvider>();

        var changeRequest = new LoggersRequest("foobar", "WARN");
        LoggersResponse? response = await handler.InvokeAsync(changeRequest, TestContext.Current.CancellationToken);

        response.Should().BeNull();

        provider.SetCategory.Should().Be("foobar");
        provider.SetMinLevel.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public async Task SetLogLevel_ReturnsExpected()
    {
        using var testContext = new SteeltoeTestContext(_testOutputHelper);
        testContext.AdditionalServices = (services, _) => services.AddLoggersActuator();

        var handler = testContext.GetRequiredService<ILoggersEndpointHandler>();

        var changeRequest = new LoggersRequest("foobar", "WARN");
        LoggersResponse? response = await handler.InvokeAsync(changeRequest, TestContext.Current.CancellationToken);

        response.Should().BeNull();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.TestResources.IO;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.Logfile;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Logfile;

public sealed class LogfileEndpointTest(ITestOutputHelper testOutputHelper) : BaseTest
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    [Fact]
    public void GetLogFilePathWithRelativePath_ReturnsPathRelativeToEntryAssemblyDirectory()
    {
        // arrange
        string directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
        string expectedFilePath = Path.Combine(directoryName, "logs/testfile.log");

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:logfile:filePath"] = "logs/testfile.log",
            ["management:endpoints:logfile:enabled"] = "true"
        };

        using var testContext = new TestContext(_testOutputHelper);

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appSettings);
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton(TestHostEnvironmentFactory.Create());
            services.ConfigureEndpointOptions<LogfileEndpointOptions, ConfigureLogfileEndpointOptions>();
            services.AddSingleton<ILogfileEndpointHandler, LogfileEndpointHandler>();
        };

        var handler = (LogfileEndpointHandler)testContext.GetRequiredService<ILogfileEndpointHandler>();

        // act
        string result = handler.GetLogFilePath();

        // assert
        Assert.NotNull(result);
        Assert.Equal(expectedFilePath, result);
    }

    [Fact]
    public void GetLogFilePathWithAbsolutePath_ReturnsAbsolutePath()
    {
        // arrange
        const string expectedFilePath = "/logs/testfile.log";

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:logfile:filePath"] = expectedFilePath,
            ["management:endpoints:logfile:enabled"] = "true"
        };

        using var testContext = new TestContext(_testOutputHelper);

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appSettings);
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton(TestHostEnvironmentFactory.Create());
            services.ConfigureEndpointOptions<LogfileEndpointOptions, ConfigureLogfileEndpointOptions>();
            services.AddSingleton<ILogfileEndpointHandler, LogfileEndpointHandler>();
        };

        var handler = (LogfileEndpointHandler)testContext.GetRequiredService<ILogfileEndpointHandler>();

        // act
        string result = handler.GetLogFilePath();

        // assert
        Assert.NotNull(result);
        Assert.Equal(expectedFilePath, result);
    }

    [Fact]
    public void GetLogFilePathWithNoConfig_ReturnsEmptyString()
    {
        // arrange
        string expectedFilePath = string.Empty;

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:logfile:enabled"] = "true"
        };

        using var testContext = new TestContext(_testOutputHelper);

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appSettings);
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton(TestHostEnvironmentFactory.Create());
            services.ConfigureEndpointOptions<LogfileEndpointOptions, ConfigureLogfileEndpointOptions>();
            services.AddSingleton<ILogfileEndpointHandler, LogfileEndpointHandler>();
        };

        var handler = (LogfileEndpointHandler)testContext.GetRequiredService<ILogfileEndpointHandler>();

        // act
        string result = handler.GetLogFilePath();

        // assert
        Assert.NotNull(result);
        Assert.Equal(expectedFilePath, result);
    }

    [Fact]
    public void Options_ReturnsExpected()
    {
        // arrange
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:logfile:filePath"] = "logs/testfile.log",
            ["management:endpoints:logfile:enabled"] = "true"
        };

        using var testContext = new TestContext(_testOutputHelper);

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appSettings);
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton(TestHostEnvironmentFactory.Create());
            services.ConfigureEndpointOptions<LogfileEndpointOptions, ConfigureLogfileEndpointOptions>();
            services.AddSingleton<ILogfileEndpointHandler, LogfileEndpointHandler>();
        };

        var handler = (LogfileEndpointHandler)testContext.GetRequiredService<ILogfileEndpointHandler>();

        // act
        var options = (handler.Options as LogfileEndpointOptions)!;

        // assert
        options.Id.Should().Be("logfile");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Restricted);
        options.Path.Should().Be("logfile");
        options.FilePath.Should().Be("logs/testfile.log");
        options.AllowedVerbs.Should().Contain("Get");
        options.AllowedVerbs.Should().HaveCount(1);
    }

    [Fact]
    public async Task Invoke_ReturnsExpected()
    {
        // arrange
        const string expectedLogFileContents = "This is a test log file content.";
        using var tempLogFile = new TempFile();
        await File.WriteAllTextAsync(tempLogFile.FullPath, expectedLogFileContents);

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:logfile:filePath"] = tempLogFile.FullPath,
            ["management:endpoints:logfile:enabled"] = "true"
        };

        using var testContext = new TestContext(_testOutputHelper);

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appSettings);
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton(TestHostEnvironmentFactory.Create());
            services.ConfigureEndpointOptions<LogfileEndpointOptions, ConfigureLogfileEndpointOptions>();
            services.AddSingleton<ILogfileEndpointHandler, LogfileEndpointHandler>();
        };

        var handler = (LogfileEndpointHandler)testContext.GetRequiredService<ILogfileEndpointHandler>();

        // act
        string logFileContents = await handler.InvokeAsync("an object", CancellationToken.None);

        // assert
        logFileContents.Should().Be(expectedLogFileContents);
    }

    [Fact]
    public async Task Invoke_ReturnsEmptyStringWhenNoLogFileSpecified()
    {
        // arrange
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:logfile:enabled"] = "true"
        };

        using var testContext = new TestContext(_testOutputHelper);

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appSettings);
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton(TestHostEnvironmentFactory.Create());
            services.ConfigureEndpointOptions<LogfileEndpointOptions, ConfigureLogfileEndpointOptions>();
            services.AddSingleton<ILogfileEndpointHandler, LogfileEndpointHandler>();
        };

        var handler = (LogfileEndpointHandler)testContext.GetRequiredService<ILogfileEndpointHandler>();

        // act
        string logFileContents = await handler.InvokeAsync("an object", CancellationToken.None);

        // assert
        logFileContents.Should().Be(string.Empty);
    }
}

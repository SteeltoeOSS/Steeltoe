// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.TestResources.IO;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.LogFile;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Logfile;

public sealed class LogFileEndpointTest(ITestOutputHelper testOutputHelper) : BaseTest
{
    private readonly string[] _expectedAllowedVerbs = ["Get", "Head"];
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    [Fact]
    public void GetLogFilePathWithRelativePath_ReturnsPathRelativeToEntryAssemblyDirectory()
    {
        string directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
        string expectedFilePath = Path.Combine(directoryName, "logs/testfile.log");

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:logfile:filePath"] = "logs/testfile.log",
            ["management:endpoints:logfile:enabled"] = "true"
        };

        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appSettings);
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.ConfigureEndpointOptions<LogFileEndpointOptions, ConfigureLogFileEndpointOptions>();
            services.AddSingleton<ILogFileEndpointHandler, LogFileEndpointHandler>();
        };

        var handler = (LogFileEndpointHandler)testContext.GetRequiredService<ILogFileEndpointHandler>();
        string result = handler.GetLogFilePath();

        Assert.NotNull(result);
        Assert.Equal(expectedFilePath, result);
    }

    [Fact]
    public void GetLogFilePathWithAbsolutePath_ReturnsAbsolutePath()
    {
        const string expectedFilePath = "/logs/testfile.log";

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:logfile:filePath"] = expectedFilePath,
            ["management:endpoints:logfile:enabled"] = "true"
        };

        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appSettings);
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.ConfigureEndpointOptions<LogFileEndpointOptions, ConfigureLogFileEndpointOptions>();
            services.AddSingleton<ILogFileEndpointHandler, LogFileEndpointHandler>();
        };

        var handler = (LogFileEndpointHandler)testContext.GetRequiredService<ILogFileEndpointHandler>();
        string result = handler.GetLogFilePath();

        Assert.NotNull(result);
        Assert.Equal(expectedFilePath, result);
    }

    [Fact]
    public void GetLogFilePathWithNoConfig_ReturnsEmptyString()
    {
        string expectedFilePath = string.Empty;

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:logfile:enabled"] = "true"
        };

        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appSettings);
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.ConfigureEndpointOptions<LogFileEndpointOptions, ConfigureLogFileEndpointOptions>();
            services.AddSingleton<ILogFileEndpointHandler, LogFileEndpointHandler>();
        };

        var handler = (LogFileEndpointHandler)testContext.GetRequiredService<ILogFileEndpointHandler>();
        string result = handler.GetLogFilePath();

        Assert.NotNull(result);
        Assert.Equal(expectedFilePath, result);
    }

    [Fact]
    public void Options_ReturnsExpected()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:logfile:filePath"] = "logs/testfile.log",
            ["management:endpoints:logfile:enabled"] = "true"
        };

        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appSettings);
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddLogFileActuator();
        };

        var handler = (LogFileEndpointHandler)testContext.GetRequiredService<ILogFileEndpointHandler>();
        var options = (handler.Options as LogFileEndpointOptions)!;

        options.Id.Should().Be("logfile");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Restricted);
        options.Path.Should().Be("logfile");
        options.FilePath.Should().Be("logs/testfile.log");
        options.AllowedVerbs.Should().Contain(_expectedAllowedVerbs);
    }

    [Fact]
    public async Task Invoke_ReturnsExpected()
    {
        const string expectedLogFileContents = "This is a test log file content.";
        using var tempLogFile = new TempFile();
        await File.WriteAllTextAsync(tempLogFile.FullPath, expectedLogFileContents, TestContext.Current.CancellationToken);

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:logfile:filePath"] = tempLogFile.FullPath,
            ["management:endpoints:logfile:enabled"] = "true"
        };

        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appSettings);
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddLogFileActuator();
        };

        var handler = (LogFileEndpointHandler)testContext.GetRequiredService<ILogFileEndpointHandler>();
        var logFileContents = await handler.InvokeAsync(new LogFileEndpointRequest(), TestContext.Current.CancellationToken);

        logFileContents.Content.Should().Be(expectedLogFileContents);
    }

    [Fact]
    public async Task Invoke_ReturnsEmptyStringWhenNoLogFileSpecified()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:logfile:enabled"] = "true"
        };

        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appSettings);
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddLogFileActuator();
        };

        var handler = (LogFileEndpointHandler)testContext.GetRequiredService<ILogFileEndpointHandler>();
        var logFileContents = await handler.InvokeAsync(new LogFileEndpointRequest(), TestContext.Current.CancellationToken);

        logFileContents.Content.Should().Be(null);
    }
}

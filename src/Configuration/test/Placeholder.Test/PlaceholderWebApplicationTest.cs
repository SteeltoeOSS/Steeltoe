// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Common.TestResources.IO;
using Xunit.Abstractions;

namespace Steeltoe.Configuration.Placeholder.Test;

public sealed class PlaceholderWebApplicationTest : IDisposable
{
    private readonly LoggerFactory _loggerFactory;

    public PlaceholderWebApplicationTest(ITestOutputHelper testOutputHelper)
    {
        var loggerProvider = new XunitLoggerProvider(testOutputHelper, category => category.StartsWith("Steeltoe", StringComparison.Ordinal));
        _loggerFactory = new LoggerFactory([loggerProvider]);
    }

    [Fact]
    public async Task Reloads_options_on_change()
    {
        const string appsettings = """
            {
              "TestRoot": {
                "AppName": "AppOne"
              }
            }
            """;

        const string fileName = "appsettings.json";
        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile(fileName, appsettings);

        var memorySettings = new Dictionary<string, string?>
        {
            ["TestRoot:Value"] = "${testRoot:appName}"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Services.AddSingleton<ILoggerFactory>(_loggerFactory);
        builder.Configuration.SetBasePath(sandbox.FullPath);
        builder.Configuration.AddInMemoryCollection(memorySettings);
        builder.Configuration.AddJsonFile(fileName, false, true);
        builder.Configuration.AddPlaceholderResolver(_loggerFactory);
        builder.Services.Configure<TestOptions>(builder.Configuration.GetSection("TestRoot"));
        builder.Services.AddSingleton<IConfigureOptions<TestOptions>, ConfigureTestOptions>();
        await using WebApplication app = builder.Build();

        var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<TestOptions>>();
        optionsMonitor.CurrentValue.Value.Should().Be("AppOne");

        await File.WriteAllTextAsync(path, """
        {
          "TestRoot": {
            "AppName": "AppTwo"
          }
        }
        """);

        await Task.Delay(TimeSpan.FromSeconds(2));

        optionsMonitor.CurrentValue.Value.Should().Be("AppTwo");

        await File.WriteAllTextAsync(path, """
        {
          "TestRoot": {
            "AppName": "AppThree"
          }
        }
        """);

        await Task.Delay(TimeSpan.FromSeconds(2));

        optionsMonitor.CurrentValue.Value.Should().Be("AppThree");
    }

    [Fact]
    public void Can_rebuild_sources()
    {
        var template = new Dictionary<string, string?>
        {
            { "placeholder", "${value}" }
        };

        var valueProviderA = new Dictionary<string, string?>
        {
            { "value", "A" }
        };

        var valueProviderB = new Dictionary<string, string?>
        {
            { "value", "B" }
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(template);
        builder.Configuration.AddInMemoryCollection(valueProviderA);
        builder.Configuration.AddInMemoryCollection(valueProviderB);
        builder.Configuration.AddPlaceholderResolver(_loggerFactory);

        builder.Configuration["placeholder"].Should().Be("B");

        var configurationBuilder = (IConfigurationBuilder)builder.Configuration;

        PlaceholderConfigurationSource placeholderSource = configurationBuilder.Sources.OfType<PlaceholderConfigurationSource>().Single();
        placeholderSource.Sources.RemoveAt(2);

        // Trigger a rebuild of all configuration sources in ConfigurationManager, which creates new providers and disposes the existing ones.
        configurationBuilder.Properties.Remove(string.Empty);

        builder.Configuration["placeholder"].Should().Be("A");
    }

    [Fact]
    public async Task Can_substitute_across_multiple_sources()
    {
        const string appsettingsJsonFileName = "appsettings.json";
        const string appsettingsXmlFileName = "appsettings.xml";
        const string appsettingsIniFileName = "appsettings.ini";

        const string appsettingsJsonContent = """
            {
              "JsonTestRoot": {
                "JsonSubLevel": {
                  "JsonKey": "JsonValue",
                  "XmlSource": "JsonTo${XmlTestRoot:XmlSubLevel:XmlKey}"
                }
              }
            }
            """;

        const string appsettingsXmlContent = """
            <settings>
            	<XmlTestRoot>
            		<XmlSubLevel>
            			<XmlKey>XmlValue</XmlKey>
            			<IniSource>XmlTo${IniTestRoot:IniSubLevel:IniKey}</IniSource>
            		</XmlSubLevel>
            	</XmlTestRoot>
            </settings>
            """;

        const string appsettingsIniContent = """
            [IniTestRoot:IniSubLevel]
            IniKey=IniValue
            CmdSource=IniTo${CmdTestRoot:CmdSubLevel:CmdKey}
            """;

        string[] appsettingsCommandLine =
        [
            "--CmdTestRoot:CmdSubLevel:CmdKey=CmdValue",
            "--CmdTestRoot:CmdSubLevel:JsonSource=CmdTo${JsonTestRoot:JsonSubLevel:JsonKey}"
        ];

        using var sandbox = new Sandbox();
        sandbox.CreateFile(appsettingsJsonFileName, appsettingsJsonContent);
        sandbox.CreateFile(appsettingsXmlFileName, appsettingsXmlContent);
        sandbox.CreateFile(appsettingsIniFileName, appsettingsIniContent);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.SetBasePath(sandbox.FullPath);
        builder.Configuration.AddJsonFile(appsettingsJsonFileName);
        builder.Configuration.AddXmlFile(appsettingsXmlFileName);
        builder.Configuration.AddIniFile(appsettingsIniFileName);
        builder.Configuration.AddCommandLine(appsettingsCommandLine);
        builder.Configuration.AddPlaceholderResolver(_loggerFactory);

        await using WebApplication app = builder.Build();
        var configuration = app.Services.GetRequiredService<IConfiguration>();

        configuration["JsonTestRoot:JsonSubLevel:JsonKey"].Should().Be("JsonValue");
        configuration["XmlTestRoot:XmlSubLevel:XmlKey"].Should().Be("XmlValue");
        configuration["IniTestRoot:IniSubLevel:IniKey"].Should().Be("IniValue");
        configuration["CmdTestRoot:CmdSubLevel:CmdKey"].Should().Be("CmdValue");

        configuration["JsonTestRoot:JsonSubLevel:XmlSource"].Should().Be("JsonToXmlValue");
        configuration["XmlTestRoot:XmlSubLevel:IniSource"].Should().Be("XmlToIniValue");
        configuration["IniTestRoot:IniSubLevel:CmdSource"].Should().Be("IniToCmdValue");
        configuration["CmdTestRoot:CmdSubLevel:JsonSource"].Should().Be("CmdToJsonValue");
    }

    public void Dispose()
    {
        _loggerFactory.Dispose();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Utils.IO;
using Xunit;

namespace Steeltoe.Configuration.Placeholder.Test;

public sealed class PlaceholderResolverExtensionsTest
{
    [Fact]
    public void ConfigurePlaceholderResolver_ConfiguresIConfiguration_ReplacesExisting()
    {
        var settings = new Dictionary<string, string?>
        {
            { "key1", "value1" },
            { "key2", "${key1?notfound}" },
            { "key3", "${nokey?notfound}" },
            { "key4", "${nokey}" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        IConfigurationRoot config1 = builder.Build();

        IWebHostBuilder hostBuilder = new WebHostBuilder();
        hostBuilder.UseStartup<TestServerStartup>();
        hostBuilder.UseConfiguration(config1);
        hostBuilder.ConfigureServices((context, services) => services.ConfigurePlaceholderResolver(context.Configuration));

        using var server = new TestServer(hostBuilder);
        var config2 = server.Services.GetRequiredService<IConfiguration>();
        Assert.NotSame(config1, config2);

        Assert.Null(config2["nokey"]);
        Assert.Equal("value1", config2["key1"]);
        Assert.Equal("value1", config2["key2"]);
        Assert.Equal("notfound", config2["key3"]);
        Assert.Equal("${nokey}", config2["key4"]);
    }

    [Fact]
    public void AddPlaceholderResolver_WebHostBuilder_WrapsApplicationsConfiguration()
    {
        const string appsettingsJson = @"
                {
                    ""spring"": {
                        ""json"": {
                            ""name"": ""myName""
                    },
                      ""cloud"": {
                        ""config"": {
                            ""name"" : ""${spring:xml:name?noname}"",
                        }
                      }
                    }
                }";

        const string appsettingsXml = @"
                <settings>
                    <spring>
                        <xml>
                            <name>${spring:ini:name?noName}</name>
                        </xml>
                    </spring>
                </settings>";

        const string appsettingsIni = @"
[spring:ini]
    name=${spring:line:name?noName}
";

        string[] appsettingsLine = ["--spring:line:name=${spring:json:name?noName}"];

        using var sandbox = new Sandbox();
        string jsonPath = sandbox.CreateFile("appsettings.json", appsettingsJson);
        string jsonFileName = Path.GetFileName(jsonPath);
        string xmlPath = sandbox.CreateFile("appsettings.xml", appsettingsXml);
        string xmlFileName = Path.GetFileName(xmlPath);
        string iniPath = sandbox.CreateFile("appsettings.ini", appsettingsIni);
        string iniFileName = Path.GetFileName(iniPath);

        string directory = Path.GetDirectoryName(jsonPath)!;

        IWebHostBuilder hostBuilder = new WebHostBuilder().UseStartup<TestServerStartup>().ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.SetBasePath(directory);
            configurationBuilder.AddJsonFile(jsonFileName);
            configurationBuilder.AddXmlFile(xmlFileName);
            configurationBuilder.AddIniFile(iniFileName);
            configurationBuilder.AddCommandLine(appsettingsLine);
            configurationBuilder.AddPlaceholderResolver();
        });

        using var server = new TestServer(hostBuilder);
        var configuration = server.Services.GetRequiredService<IConfiguration>();
        Assert.Equal("myName", configuration["spring:cloud:config:name"]);
    }

    [Fact]
    public void AddPlaceholderResolver_HostBuilder_WrapsApplicationsConfiguration()
    {
        const string appsettingsJson = @"
                {
                    ""spring"": {
                        ""json"": {
                            ""name"": ""myName""
                    },
                      ""cloud"": {
                        ""config"": {
                            ""name"" : ""${spring:xml:name?noname}"",
                        }
                      }
                    }
                }";

        const string appsettingsXml = @"
                <settings>
                    <spring>
                        <xml>
                            <name>${spring:json:name?noName}</name>
                        </xml>
                    </spring>
                </settings>";

        using var sandbox = new Sandbox();
        string jsonPath = sandbox.CreateFile("appsettings.json", appsettingsJson);
        string jsonFileName = Path.GetFileName(jsonPath);
        string xmlPath = sandbox.CreateFile("appsettings.xml", appsettingsXml);
        string xmlFileName = Path.GetFileName(xmlPath);
        string directory = Path.GetDirectoryName(jsonPath)!;

        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(configure => configure.UseTestServer()).ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.SetBasePath(directory);
            configurationBuilder.AddJsonFile(jsonFileName);
            configurationBuilder.AddXmlFile(xmlFileName);
            configurationBuilder.AddPlaceholderResolver();
        });

        using TestServer server = hostBuilder.Build().GetTestServer();
        var configuration = server.Services.GetRequiredService<IConfiguration>();
        Assert.Equal("myName", configuration["spring:cloud:config:name"]);
    }

    [Fact]
    public void AddPlaceholderResolverViaWebApplicationBuilderWorks()
    {
        const string appsettingsJson = @"
            {
                ""spring"": {
                    ""json"": {
                        ""name"": ""myName""
                },
                  ""cloud"": {
                    ""config"": {
                        ""name"" : ""${spring:xml:name?noname}"",
                    }
                  }
                }
            }";

        const string appsettingsXml = @"
            <settings>
                <spring>
                    <xml>
                        <name>${spring:json:name?noName}</name>
                    </xml>
                </spring>
            </settings>";

        using var sandbox = new Sandbox();
        string jsonPath = sandbox.CreateFile("appsettings.json", appsettingsJson);
        string jsonFileName = Path.GetFileName(jsonPath);
        string xmlPath = sandbox.CreateFile("appsettings.xml", appsettingsXml);
        string xmlFileName = Path.GetFileName(xmlPath);
        string directory = Path.GetDirectoryName(jsonPath)!;

        WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.Host.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        hostBuilder.Configuration.SetBasePath(directory);
        hostBuilder.Configuration.AddJsonFile(jsonFileName);
        hostBuilder.Configuration.AddXmlFile(xmlFileName);
        hostBuilder.Configuration.AddPlaceholderResolver();

        using WebApplication server = hostBuilder.Build();
        var configuration = server.Services.GetRequiredService<IConfiguration>();
        Assert.Equal("myName", configuration["spring:cloud:config:name"]);
    }
}

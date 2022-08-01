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

namespace Steeltoe.Extensions.Configuration.Placeholder.Test;

public class PlaceholderResolverExtensionsTest
{
    [Fact]
    public void ConfigurePlaceholderResolver_ThrowsIfNulls()
    {
        const IServiceCollection services = null;
        const IConfigurationRoot config = null;

        Assert.Throws<ArgumentNullException>(() => services.ConfigurePlaceholderResolver(config));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().ConfigurePlaceholderResolver(config));
    }

    [Fact]
    public void ConfigurePlaceholderResolver_ConfiguresIConfiguration_ReplacesExisting()
    {
        var settings = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "${key1?notfound}" },
            { "key3", "${nokey?notfound}" },
            { "key4", "${nokey}" },
        };
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        var config1 = builder.Build();

        var hostBuilder = new WebHostBuilder()
            .UseStartup<TestServerStartup>()
            .UseConfiguration(config1);

        using var server = new TestServer(hostBuilder);
        var services = TestServerStartup.ServiceProvider;
        var config2 = services.GetServices<IConfiguration>().SingleOrDefault();
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
        var appsettingsJson = @"
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

        var appsettingsXml = @"
                <settings>
                    <spring>
                        <xml>
                            <name>${spring:ini:name?noName}</name>
                        </xml>
                    </spring>
                </settings>";

        var appsettingsIni = @"
[spring:ini]
    name=${spring:line:name?noName}
";
        var appsettingsLine = new[]
        {
            "--spring:line:name=${spring:json:name?noName}"
        };
        using var sandbox = new Sandbox();
        var jsonPath = sandbox.CreateFile("appsettings.json", appsettingsJson);
        var jsonFileName = Path.GetFileName(jsonPath);
        var xmlPath = sandbox.CreateFile("appsettings.xml", appsettingsXml);
        var xmlFileName = Path.GetFileName(xmlPath);
        var iniPath = sandbox.CreateFile("appsettings.ini", appsettingsIni);
        var iniFileName = Path.GetFileName(iniPath);

        var directory = Path.GetDirectoryName(jsonPath);

        var hostBuilder = new WebHostBuilder()
            .UseStartup<TestServerStartup1>()
            .ConfigureAppConfiguration(configurationBuilder =>
            {
                configurationBuilder.SetBasePath(directory);
                configurationBuilder.AddJsonFile(jsonFileName);
                configurationBuilder.AddXmlFile(xmlFileName);
                configurationBuilder.AddIniFile(iniFileName);
                configurationBuilder.AddCommandLine(appsettingsLine);
            })
            .AddPlaceholderResolver();

        using var server = new TestServer(hostBuilder);
        var services = TestServerStartup1.ServiceProvider;
        var config = services.GetServices<IConfiguration>().SingleOrDefault();
        Assert.Equal("myName", config["spring:cloud:config:name"]);
    }

    [Fact]
    public void AddPlaceholderResolver_HostBuilder_WrapsApplicationsConfiguration()
    {
        var appsettingsJson = @"
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
        var appsettingsXml = @"
                <settings>
                    <spring>
                        <xml>
                            <name>${spring:json:name?noName}</name>
                        </xml>
                    </spring>
                </settings>";
        using var sandbox = new Sandbox();
        var jsonPath = sandbox.CreateFile("appsettings.json", appsettingsJson);
        var jsonFileName = Path.GetFileName(jsonPath);
        var xmlPath = sandbox.CreateFile("appsettings.xml", appsettingsXml);
        var xmlFileName = Path.GetFileName(xmlPath);
        var directory = Path.GetDirectoryName(jsonPath);

        var hostBuilder = new HostBuilder().ConfigureWebHost(configure => configure.UseTestServer())
            .ConfigureAppConfiguration(configurationBuilder =>
            {
                configurationBuilder.SetBasePath(directory);
                configurationBuilder.AddJsonFile(jsonFileName);
                configurationBuilder.AddXmlFile(xmlFileName);
            })
            .AddPlaceholderResolver();

        using var server = hostBuilder.Build().GetTestServer();
        var config = server.Services.GetServices<IConfiguration>().SingleOrDefault();
        Assert.Equal("myName", config["spring:cloud:config:name"]);
    }

    [Fact]
    public void AddPlaceholderResolverViaWebApplicationBuilderWorks()
    {
        var appsettingsJson = @"
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
        var appsettingsXml = @"
            <settings>
                <spring>
                    <xml>
                        <name>${spring:json:name?noName}</name>
                    </xml>
                </spring>
            </settings>";
        using var sandbox = new Sandbox();
        var jsonPath = sandbox.CreateFile("appsettings.json", appsettingsJson);
        var jsonFileName = Path.GetFileName(jsonPath);
        var xmlPath = sandbox.CreateFile("appsettings.xml", appsettingsXml);
        var xmlFileName = Path.GetFileName(xmlPath);
        var directory = Path.GetDirectoryName(jsonPath);

        var hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.Configuration.SetBasePath(directory);
        hostBuilder.Configuration.AddJsonFile(jsonFileName);
        hostBuilder.Configuration.AddXmlFile(xmlFileName);
        hostBuilder.AddPlaceholderResolver();

        using var server = hostBuilder.Build();
        var config = server.Services.GetServices<IConfiguration>().First();
        Assert.Equal("myName", config["spring:cloud:config:name"]);
    }
}

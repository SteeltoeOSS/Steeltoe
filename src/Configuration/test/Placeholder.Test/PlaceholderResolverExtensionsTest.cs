// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Utils.IO;
using Xunit;

namespace Steeltoe.Extensions.Configuration.Placeholder.Test;

public sealed class PlaceholderResolverExtensionsTest
{
    [Fact]
    public void ConfigurePlaceholderResolver_WithServiceCollection_ThrowsIfNulls()
    {
        const IServiceCollection nullServiceCollection = null;
        var serviceCollection = new ServiceCollection();
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        var loggerFactory = NullLoggerFactory.Instance;

        Assert.Throws<ArgumentNullException>(() => nullServiceCollection.ConfigurePlaceholderResolver(configuration, loggerFactory));
        Assert.Throws<ArgumentNullException>(() => serviceCollection.ConfigurePlaceholderResolver(null, loggerFactory));
        Assert.Throws<ArgumentNullException>(() => serviceCollection.ConfigurePlaceholderResolver(configuration, null));
    }

    [Fact]
    public void AddPlaceholderResolver_WithWebHostBuilder_ThrowsIfNulls()
    {
        const IWebHostBuilder nullWebHostBuilder = null;
        var webHostBuilder = new WebHostBuilder();
        var loggerFactory = NullLoggerFactory.Instance;

        Assert.Throws<ArgumentNullException>(() => nullWebHostBuilder.AddPlaceholderResolver(loggerFactory));
        Assert.Throws<ArgumentNullException>(() => webHostBuilder.AddPlaceholderResolver(null));
    }

    [Fact]
    public void AddPlaceholderResolver_WithHostBuilder_ThrowsIfNulls()
    {
        const IHostBuilder nullHostBuilder = null;
        var hostBuilder = new HostBuilder();
        var loggerFactory = NullLoggerFactory.Instance;

        Assert.Throws<ArgumentNullException>(() => nullHostBuilder.AddPlaceholderResolver(loggerFactory));
        Assert.Throws<ArgumentNullException>(() => hostBuilder.AddPlaceholderResolver(null));
    }

    [Fact]
    public void AddPlaceholderResolver_WebApplicationBuilder_ThrowsIfNulls()
    {
        const WebApplicationBuilder nullWebApplicationBuilder = null;
        WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder();
        var loggerFactory = NullLoggerFactory.Instance;

        Assert.Throws<ArgumentNullException>(() => nullWebApplicationBuilder.AddPlaceholderResolver(loggerFactory));
        Assert.Throws<ArgumentNullException>(() => webApplicationBuilder.AddPlaceholderResolver(null));
    }

    [Fact]
    public void ConfigurePlaceholderResolver_ThrowsIfNulls()
    {
        const IServiceCollection nullServices = null;
        var serviceCollection = new ServiceCollection();
        var loggerFactory = NullLoggerFactory.Instance;
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();

        Assert.Throws<ArgumentNullException>(() => nullServices.ConfigurePlaceholderResolver(configuration, loggerFactory));
        Assert.Throws<ArgumentNullException>(() => serviceCollection.ConfigurePlaceholderResolver(null, loggerFactory));
        Assert.Throws<ArgumentNullException>(() => serviceCollection.ConfigurePlaceholderResolver(configuration, null));
    }

    [Fact]
    public void ConfigurePlaceholderResolver_ConfiguresIConfiguration_ReplacesExisting()
    {
        var settings = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "${key1?notfound}" },
            { "key3", "${nokey?notfound}" },
            { "key4", "${nokey}" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        IConfigurationRoot config1 = builder.Build();

        IWebHostBuilder hostBuilder = new WebHostBuilder().UseStartup<StartupForConfigurePlaceholderResolver>().UseConfiguration(config1);

        using var server = new TestServer(hostBuilder);
        IServiceProvider services = StartupForConfigurePlaceholderResolver.ServiceProvider;
        IConfiguration config2 = services.GetServices<IConfiguration>().SingleOrDefault();
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

        string[] appsettingsLine =
        {
            "--spring:line:name=${spring:json:name?noName}"
        };

        using var sandbox = new Sandbox();
        string jsonPath = sandbox.CreateFile("appsettings.json", appsettingsJson);
        string jsonFileName = Path.GetFileName(jsonPath);
        string xmlPath = sandbox.CreateFile("appsettings.xml", appsettingsXml);
        string xmlFileName = Path.GetFileName(xmlPath);
        string iniPath = sandbox.CreateFile("appsettings.ini", appsettingsIni);
        string iniFileName = Path.GetFileName(iniPath);

        string directory = Path.GetDirectoryName(jsonPath);

        IWebHostBuilder hostBuilder = new WebHostBuilder().UseStartup<StartupForAddPlaceholderResolver>().ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.SetBasePath(directory);
            configurationBuilder.AddJsonFile(jsonFileName);
            configurationBuilder.AddXmlFile(xmlFileName);
            configurationBuilder.AddIniFile(iniFileName);
            configurationBuilder.AddCommandLine(appsettingsLine);
        }).AddPlaceholderResolver();

        using var server = new TestServer(hostBuilder);
        IServiceProvider services = StartupForAddPlaceholderResolver.ServiceProvider;
        IConfiguration configuration = services.GetServices<IConfiguration>().SingleOrDefault();
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
        string directory = Path.GetDirectoryName(jsonPath);

        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(configure => configure.UseTestServer()).ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.SetBasePath(directory);
            configurationBuilder.AddJsonFile(jsonFileName);
            configurationBuilder.AddXmlFile(xmlFileName);
        }).AddPlaceholderResolver();

        using TestServer server = hostBuilder.Build().GetTestServer();
        IConfiguration configuration = server.Services.GetServices<IConfiguration>().SingleOrDefault();
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
        string directory = Path.GetDirectoryName(jsonPath);

        WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.Configuration.SetBasePath(directory);
        hostBuilder.Configuration.AddJsonFile(jsonFileName);
        hostBuilder.Configuration.AddXmlFile(xmlFileName);
        hostBuilder.AddPlaceholderResolver();

        using WebApplication server = hostBuilder.Build();
        IConfiguration configuration = server.Services.GetServices<IConfiguration>().First();
        Assert.Equal("myName", configuration["spring:cloud:config:name"]);
    }
}

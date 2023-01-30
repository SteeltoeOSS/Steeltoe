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
using Moq;
using Steeltoe.Common.Utils.IO;
using Xunit;

namespace Steeltoe.Configuration.Encryption.Test;

public sealed class EncryptionResolverExtensionsTest
{
    private readonly Mock<ITextDecryptor> _decryptorMock;

    public EncryptionResolverExtensionsTest()
    {
        _decryptorMock = new Mock<ITextDecryptor>();
    }
    [Fact]
    public void ConfigureEncryptionResolver_WithServiceCollection_ThrowsIfNulls()
    {
        const IServiceCollection nullServiceCollection = null;
        var serviceCollection = new ServiceCollection();
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        var loggerFactory = NullLoggerFactory.Instance;

        Assert.Throws<ArgumentNullException>(() => nullServiceCollection.ConfigureEncryptionResolver(configuration, loggerFactory, _decryptorMock.Object));
        Assert.Throws<ArgumentNullException>(() => serviceCollection.ConfigureEncryptionResolver(null, loggerFactory,_decryptorMock.Object));
        Assert.Throws<ArgumentNullException>(() => serviceCollection.ConfigureEncryptionResolver(configuration, null,_decryptorMock.Object));
    }

    [Fact]
    public void AddEncryptionResolver_WithWebHostBuilder_ThrowsIfNulls()
    {
        const IWebHostBuilder nullWebHostBuilder = null;
        var webHostBuilder = new WebHostBuilder();
        var loggerFactory = NullLoggerFactory.Instance;

        Assert.Throws<ArgumentNullException>(() => nullWebHostBuilder.AddEncryptionResolver(loggerFactory, _decryptorMock.Object));
        Assert.Throws<ArgumentNullException>(() => webHostBuilder.AddEncryptionResolver(null));
    }

    [Fact]
    public void AddEncryptionResolver_WithHostBuilder_ThrowsIfNulls()
    {
        const IHostBuilder nullHostBuilder = null;
        var hostBuilder = new HostBuilder();
        var loggerFactory = NullLoggerFactory.Instance;

        Assert.Throws<ArgumentNullException>(() => nullHostBuilder.AddEncryptionResolver(loggerFactory, _decryptorMock.Object));
        Assert.Throws<ArgumentNullException>(() => hostBuilder.AddEncryptionResolver(null));
    }

    [Fact]
    public void AddEncryptionResolver_WebApplicationBuilder_ThrowsIfNulls()
    {
        const WebApplicationBuilder nullWebApplicationBuilder = null;
        WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder();
        var loggerFactory = NullLoggerFactory.Instance;

        Assert.Throws<ArgumentNullException>(() => nullWebApplicationBuilder.AddEncryptionResolver(loggerFactory, _decryptorMock.Object));
        Assert.Throws<ArgumentNullException>(() => webApplicationBuilder.AddEncryptionResolver(null));
    }

    [Fact]
    public void ConfigureEncryptionResolver_ThrowsIfNulls()
    {
        const IServiceCollection nullServices = null;
        var serviceCollection = new ServiceCollection();
        var loggerFactory = NullLoggerFactory.Instance;
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();

        Assert.Throws<ArgumentNullException>(() => nullServices.ConfigureEncryptionResolver(configuration, loggerFactory, _decryptorMock.Object));
        Assert.Throws<ArgumentNullException>(() => serviceCollection.ConfigureEncryptionResolver(null, loggerFactory, _decryptorMock.Object));
        Assert.Throws<ArgumentNullException>(() => serviceCollection.ConfigureEncryptionResolver(configuration, null, _decryptorMock.Object));
    }

    [Fact]
    public void ConfigureEncryptionResolver_ConfiguresIConfiguration_ReplacesExisting()
    {
        var settings = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "{cipher}somecipher" },
            { "key3", "{cipher:keyalias}somekeyaliascipher" },
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        IConfigurationRoot config1 = builder.Build();

        IWebHostBuilder hostBuilder = new WebHostBuilder().UseStartup<StartupForConfigureEncryptionResolver>().UseConfiguration(config1);

        using var server = new TestServer(hostBuilder);
        IServiceProvider services = StartupForConfigureEncryptionResolver.ServiceProvider;
        IConfiguration config2 = services.GetServices<IConfiguration>().SingleOrDefault();
        Assert.NotSame(config1, config2);

        Assert.Null(config2["nokey"]);
        Assert.Equal("value1", config2["key1"]);
        Assert.Equal("DECRYPTED", config2["key2"]);
        Assert.Equal("DECRYPTEDWITHALIAS", config2["key3"]);
    }
}

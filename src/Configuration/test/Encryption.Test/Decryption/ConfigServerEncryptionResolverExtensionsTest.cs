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
using Steeltoe.Configuration.Encryption.Decryption;
using Xunit;

namespace Steeltoe.Configuration.Encryption.Test.Decryption;

public sealed class ConfigServerEncryptionResolverExtensionsTest
{
    [Fact]
    public void ConfigureEncryptionResolver_WithServiceCollection_ThrowsIfNulls()
    {
        const IServiceCollection nullServiceCollection = null;
        var serviceCollection = new ServiceCollection();
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        var loggerFactory = NullLoggerFactory.Instance;

        Assert.Throws<ArgumentNullException>(() => nullServiceCollection.ConfigureConfigServerEncryptionResolver(configuration, loggerFactory));
        Assert.Throws<ArgumentNullException>(() => serviceCollection.ConfigureConfigServerEncryptionResolver(null, loggerFactory));
    }

    [Fact]
    public void AddEncryptionResolver_WithWebHostBuilder_ThrowsIfNulls()
    {
        const IWebHostBuilder nullWebHostBuilder = null;
        var webHostBuilder = new WebHostBuilder();
        var loggerFactory = NullLoggerFactory.Instance;

        Assert.Throws<ArgumentNullException>(() => nullWebHostBuilder.AddEncryptionResolver(loggerFactory));
        Assert.Throws<ArgumentNullException>(() => webHostBuilder.AddEncryptionResolver(null));
    }

    [Fact]
    public void AddEncryptionResolver_WithHostBuilder_ThrowsIfNulls()
    {
        const IHostBuilder nullHostBuilder = null;
        var hostBuilder = new HostBuilder();
        var loggerFactory = NullLoggerFactory.Instance;

        Assert.Throws<ArgumentNullException>(() => nullHostBuilder.AddEncryptionResolver(loggerFactory));
        Assert.Throws<ArgumentNullException>(() => Encryption.Decryption.ConfigServerEncryptionResolverExtensions.AddEncryptionResolver(hostBuilder, null));
    }

    [Fact]
    public void AddEncryptionResolver_WebApplicationBuilder_ThrowsIfNulls()
    {
        const WebApplicationBuilder nullWebApplicationBuilder = null;
        WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder();
        var loggerFactory = NullLoggerFactory.Instance;

        Assert.Throws<ArgumentNullException>(() => nullWebApplicationBuilder.AddEncryptionResolver(loggerFactory));
        Assert.Throws<ArgumentNullException>(() => Encryption.Decryption.ConfigServerEncryptionResolverExtensions.AddEncryptionResolver(webApplicationBuilder, null));
    }

    [Fact]
    public void ConfigureEncryptionResolver_ThrowsIfNulls()
    {
        const IServiceCollection nullServices = null;
        var serviceCollection = new ServiceCollection();
        var loggerFactory = NullLoggerFactory.Instance;
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();

        Assert.Throws<ArgumentNullException>(() => nullServices.ConfigureConfigServerEncryptionResolver(configuration, loggerFactory));
        Assert.Throws<ArgumentNullException>(() => serviceCollection.ConfigureConfigServerEncryptionResolver(null, loggerFactory));
    }

    [Fact]
    public void ConfigureEncryptionResolver_ConfiguresIConfiguration_ReplacesExisting()
    {
        var settings = new Dictionary<string, string>
        {
            { "encrypt:enabled", "true" },
            { "encrypt:keyStore:location", "./Decryption/server.jks" },
            { "encrypt:keyStore:password", "letmein" },
            { "encrypt:keyStore:alias", "mytestkey" },
            { "encrypt:rsa:strong", "false" },
            { "encrypt:rsa:algorithm", "OAEP" },
            { "encrypt:rsa:salt", "deadbeef" },
            { "key1", "value1" },
            {
                "key2",
                "{cipher}AQATBPXCmri0MCEoCam0noXJgKGlFfE/chVN7XhH1V23MqJ8sI3lI61PyvsryJP3LlfNn38gUuulMeslAs/gUCoPFPV/zD7M8x527wQUbmWD6bR0ZMJ4hu3DisK6Diw2YAOxXSsm3Zh46cPFQcowfOG1x2OXj+5uL4T+VBGdt3Nr6dHCOumkTJ1KAtaJMfASf3J8G4M27v6m4Y2EdBqP1zWwDhAZ3R0u9uTP9xYUqQiKsUeOixrhOaCvtb1Q+Zg6A41CxM4cjL3Ty6miNYLx3QkxRvfkdo0iqo7jTrWWAT1aeRV6t5U5iMlWnD4eXzad60E3ZSINhvDiB03xPPPuHKC6qUTRJEEbQFegmn/KIPMMn9WaH/JLLZNvQYMuaFszZ84AE3aQcH0be+sNFDSjHNHL"
            }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        IConfigurationRoot config1 = builder.Build();

        IWebHostBuilder hostBuilder = new WebHostBuilder().UseStartup<StartupForConfigureConfigServerEncryptionResolver>().UseConfiguration(config1);

        using var server = new TestServer(hostBuilder);
        IServiceProvider services = StartupForConfigureConfigServerEncryptionResolver.ServiceProvider;
        IConfiguration config2 = services.GetServices<IConfiguration>().SingleOrDefault();
        Assert.NotSame(config1, config2);

        Assert.Null(config2["nokey"]);
        Assert.Equal("value1", config2["key1"]);
        Assert.Equal("encrypt the world", config2["key2"]);
    }
}

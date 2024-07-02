// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Steeltoe.Configuration.Encryption.Test;

public sealed class EncryptionConfigurationExtensionsTest
{
    private readonly Mock<ITextDecryptor> _decryptorMock = new();

    [Fact]
    public void AddEncryptionResolver_AddsEncryptionResolverSourceToList()
    {
        var configurationBuilder = new ConfigurationBuilder();

        configurationBuilder.AddEncryptionResolver(_decryptorMock.Object);

        EncryptionResolverSource? encryptionSource = configurationBuilder.Sources.OfType<EncryptionResolverSource>().SingleOrDefault();
        Assert.NotNull(encryptionSource);
    }

    [Fact]
    public void AddEncryptionResolver_NoDuplicates()
    {
        var configurationBuilder = new ConfigurationBuilder();

        configurationBuilder.AddEncryptionResolver(_decryptorMock.Object);
        configurationBuilder.AddEncryptionResolver(_decryptorMock.Object);
        configurationBuilder.AddEncryptionResolver(_decryptorMock.Object);

        EncryptionResolverSource? source = configurationBuilder.Sources.OfType<EncryptionResolverSource>().SingleOrDefault();
        Assert.NotNull(source);
        Assert.NotNull(source.Sources);
        Assert.Empty(source.Sources);
    }

    [Fact]
    public void AddEncryptionResolver_CreatesProvider()
    {
        var configurationBuilder = new ConfigurationBuilder();
        var loggerFactory = new LoggerFactory();

        configurationBuilder.AddEncryptionResolver(_decryptorMock.Object, loggerFactory);
        IConfigurationRoot configuration = configurationBuilder.Build();

        EncryptionResolverProvider? provider = configuration.Providers.OfType<EncryptionResolverProvider>().SingleOrDefault();

        Assert.NotNull(provider);
    }

    [Fact]
    public void AddEncryptionResolver_ClearsSources()
    {
        var settings = new Dictionary<string, string?>
        {
            { "key1", "value1" },
            { "key2", "{cypher}something" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        builder.AddEncryptionResolver(_decryptorMock.Object);

        Assert.Single(builder.Sources);
        IConfigurationRoot configurationRoot = builder.Build();

        Assert.Single(configurationRoot.Providers);
        IConfigurationProvider provider = configurationRoot.Providers.ToList()[0];
        Assert.IsType<EncryptionResolverProvider>(provider);
    }

    [Fact]
    public void AddEncryptionResolver_WithConfiguration_ReturnsNewConfigurationWithDecryption()
    {
        _decryptorMock.Setup(x => x.Decrypt(It.IsAny<string>())).Returns((string _) => "DECRYPTED");

        var settings = new Dictionary<string, string?>
        {
            { "key1", "value1" },
            { "key2", "{cipher}something" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        IConfigurationRoot config1 = builder.Build();

        IConfiguration config2 = config1.AddEncryptionResolver(_decryptorMock.Object);
        Assert.NotSame(config1, config2);

        var root2 = (IConfigurationRoot)config2;
        Assert.Single(root2.Providers);
        IConfigurationProvider provider = root2.Providers.ToList()[0];
        Assert.IsType<EncryptionResolverProvider>(provider);

        Assert.Null(config2["nokey"]);
        Assert.Equal("value1", config2["key1"]);
        Assert.Equal("DECRYPTED", config2["key2"]);

        _decryptorMock.Verify(x => x.Decrypt("something"));
        _decryptorMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void AddEncryptionResolver_WithConfiguration_ReturnsNewConfigurationWithWithKeyAliasDecryption()
    {
        _decryptorMock.Setup(x => x.Decrypt(It.IsAny<string>(), It.IsAny<string>())).Returns((string _, string _) => "DECRYPTED");

        var settings = new Dictionary<string, string?>
        {
            { "key1", "value1" },
            { "key2", "{cipher}{key:keyalias}something" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        IConfigurationRoot config1 = builder.Build();

        IConfiguration config2 = config1.AddEncryptionResolver(_decryptorMock.Object);
        Assert.NotSame(config1, config2);

        var root2 = (IConfigurationRoot)config2;
        Assert.Single(root2.Providers);
        IConfigurationProvider provider = root2.Providers.ToList()[0];
        Assert.IsType<EncryptionResolverProvider>(provider);

        Assert.Null(config2["nokey"]);
        Assert.Equal("value1", config2["key1"]);
        Assert.Equal("DECRYPTED", config2["key2"]);

        _decryptorMock.Verify(x => x.Decrypt("something", "keyalias"));
        _decryptorMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void AddEncryptionResolver_WithConfiguration_NoDuplicates()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        IConfiguration newConfiguration = configurationRoot.AddEncryptionResolver(_decryptorMock.Object).AddEncryptionResolver(_decryptorMock.Object)
            .AddEncryptionResolver(_decryptorMock.Object);

        ConfigurationRoot newConfigurationRoot = newConfiguration.Should().BeOfType<ConfigurationRoot>().Which;
        newConfigurationRoot.Providers.Should().HaveCount(1);

        EncryptionResolverProvider? provider = newConfigurationRoot.Providers.Single().Should().BeOfType<EncryptionResolverProvider>().Which;
        provider.Providers.Should().BeEmpty();
    }
}

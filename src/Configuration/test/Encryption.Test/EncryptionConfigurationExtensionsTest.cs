// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Moq;

namespace Steeltoe.Configuration.Encryption.Test;

public sealed class EncryptionConfigurationExtensionsTest
{
    [Fact]
    public void AddEncryptionResolver_AddsEncryptionResolverSourceToList()
    {
        var configurationBuilder = new ConfigurationBuilder();

        configurationBuilder.AddEncryptionResolver();

        EncryptionResolverSource? encryptionSource = configurationBuilder.Sources.OfType<EncryptionResolverSource>().SingleOrDefault();
        Assert.NotNull(encryptionSource);
    }

    [Fact]
    public void AddEncryptionResolver_NoDuplicates()
    {
        var configurationBuilder = new ConfigurationBuilder();

        configurationBuilder.AddEncryptionResolver();
        configurationBuilder.AddEncryptionResolver();
        configurationBuilder.AddEncryptionResolver();

        EncryptionResolverSource? source = configurationBuilder.Sources.OfType<EncryptionResolverSource>().SingleOrDefault();
        Assert.NotNull(source);
        Assert.NotNull(source.Sources);
        Assert.Empty(source.Sources);
    }

    [Fact]
    public void AddEncryptionResolver_CreatesProvider()
    {
        var configurationBuilder = new ConfigurationBuilder();

        configurationBuilder.AddEncryptionResolver();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        EncryptionResolverProvider? provider = configurationRoot.Providers.OfType<EncryptionResolverProvider>().SingleOrDefault();

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
        builder.AddEncryptionResolver();

        Assert.Single(builder.Sources);
        IConfigurationRoot configurationRoot = builder.Build();

        Assert.Single(configurationRoot.Providers);
        IConfigurationProvider provider = configurationRoot.Providers.ToList()[0];
        Assert.IsType<EncryptionResolverProvider>(provider);
    }

    [Fact]
    public void AddEncryptionResolver_WithConfiguration_ReturnsNewConfigurationWithDecryption()
    {
        Mock<ITextDecryptor> decryptorMock = new();
        decryptorMock.Setup(x => x.Decrypt(It.IsAny<string>())).Returns((string _) => "DECRYPTED");

        var settings = new Dictionary<string, string?>
        {
            { "key1", "value1" },
            { "key2", "{cipher}something" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        IConfigurationRoot configuration1 = builder.Build();

        IConfiguration configuration2 = configuration1.AddEncryptionResolver(decryptorMock.Object);
        Assert.NotSame(configuration1, configuration2);

        var root2 = (IConfigurationRoot)configuration2;
        Assert.Single(root2.Providers);
        IConfigurationProvider provider = root2.Providers.ToList()[0];
        Assert.IsType<EncryptionResolverProvider>(provider);

        Assert.Null(configuration2["nokey"]);
        Assert.Equal("value1", configuration2["key1"]);
        Assert.Equal("DECRYPTED", configuration2["key2"]);

        decryptorMock.Verify(x => x.Decrypt("something"));
        decryptorMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void AddEncryptionResolver_WithConfiguration_ReturnsNewConfigurationWithWithKeyAliasDecryption()
    {
        Mock<ITextDecryptor> decryptorMock = new();
        decryptorMock.Setup(x => x.Decrypt(It.IsAny<string>(), It.IsAny<string>())).Returns((string _, string _) => "DECRYPTED");

        var settings = new Dictionary<string, string?>
        {
            { "key1", "value1" },
            { "key2", "{cipher}{key:keyalias}something" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        IConfigurationRoot configuration1 = builder.Build();

        IConfiguration configuration2 = configuration1.AddEncryptionResolver(decryptorMock.Object);
        Assert.NotSame(configuration1, configuration2);

        var root2 = (IConfigurationRoot)configuration2;
        Assert.Single(root2.Providers);
        IConfigurationProvider provider = root2.Providers.ToList()[0];
        Assert.IsType<EncryptionResolverProvider>(provider);

        Assert.Null(configuration2["nokey"]);
        Assert.Equal("value1", configuration2["key1"]);
        Assert.Equal("DECRYPTED", configuration2["key2"]);

        decryptorMock.Verify(x => x.Decrypt("something", "keyalias"));
        decryptorMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void AddEncryptionResolver_WithConfiguration_NoDuplicates()
    {
        Mock<ITextDecryptor> decryptorMock = new();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        IConfiguration newConfiguration = configurationRoot.AddEncryptionResolver(decryptorMock.Object).AddEncryptionResolver(decryptorMock.Object)
            .AddEncryptionResolver(decryptorMock.Object);

        ConfigurationRoot newConfigurationRoot = newConfiguration.Should().BeOfType<ConfigurationRoot>().Which;
        newConfigurationRoot.Providers.Should().HaveCount(1);

        EncryptionResolverProvider? provider = newConfigurationRoot.Providers.Single().Should().BeOfType<EncryptionResolverProvider>().Which;
        provider.Providers.Should().BeEmpty();
    }
}

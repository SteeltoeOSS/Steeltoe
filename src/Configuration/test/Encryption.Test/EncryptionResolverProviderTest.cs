// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Steeltoe.Configuration.Encryption.Test;

public sealed class EncryptionResolverProviderTest
{
    private readonly Mock<ITextDecryptor> _decryptorMock = new();

    [Fact]
    public void Constructor_WithConfiguration()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        var provider = new EncryptionResolverProvider(configurationRoot, _decryptorMock.Object, NullLoggerFactory.Instance);

        Assert.NotNull(provider.Configuration);
        Assert.Empty(provider.Providers);
    }

    [Fact]
    public void Constructor_WithProviders()
    {
        var providers = new List<IConfigurationProvider>();

        var provider = new EncryptionResolverProvider(providers, _decryptorMock.Object, NullLoggerFactory.Instance);

        Assert.Null(provider.Configuration);
        Assert.Same(providers, provider.Providers);
    }

    [Fact]
    public void TryGet_ReturnsResolvedDecryptedValues()
    {
        _decryptorMock.Setup(x => x.Decrypt("something")).Returns("DECRYPTED");

        var settings = new Dictionary<string, string?>
        {
            { "key1", "value1" },
            { "key2", "{cipher}something" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        List<IConfigurationProvider> providers = builder.Build().Providers.ToList();

        var holder = new EncryptionResolverProvider(providers, _decryptorMock.Object, NullLoggerFactory.Instance);

        Assert.False(holder.TryGet("nokey", out string? val));
        Assert.True(holder.TryGet("key1", out val));
        Assert.Equal("value1", val);
        Assert.True(holder.TryGet("key2", out val));
        Assert.Equal("DECRYPTED", val);
    }

    [Fact]
    public void Set_SetsValues_ReturnsResolvedDecryptedValues()
    {
        _decryptorMock.Setup(x => x.Decrypt("something")).Returns("DECRYPTED");
        _decryptorMock.Setup(x => x.Decrypt("something2")).Returns("DECRYPTED2");

        var settings = new Dictionary<string, string?>
        {
            { "key1", "value1" },
            { "key2", "{cipher}something" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        List<IConfigurationProvider> providers = builder.Build().Providers.ToList();

        var holder = new EncryptionResolverProvider(providers, _decryptorMock.Object, NullLoggerFactory.Instance);

        Assert.False(holder.TryGet("nokey", out string? val));
        Assert.True(holder.TryGet("key1", out val));
        Assert.Equal("value1", val);
        Assert.True(holder.TryGet("key2", out val));
        Assert.Equal("DECRYPTED", val);

        holder.Set("key2", "{cipher}something2");
        Assert.True(holder.TryGet("key2", out val));
        Assert.Equal("DECRYPTED2", val);

        holder.Set("key2", "nocipher");
        Assert.True(holder.TryGet("key2", out val));
        Assert.Equal("nocipher", val);
    }

    [Fact]
    public void Load_CreatesConfiguration()
    {
        var settings = new Dictionary<string, string?>
        {
            { "key1", "value1" },
            { "key2", "{cipher}encrypted" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        List<IConfigurationProvider> providers = builder.Build().Providers.ToList();

        var holder = new EncryptionResolverProvider(providers, _decryptorMock.Object, NullLoggerFactory.Instance);
        Assert.Null(holder.Configuration);
        holder.Load();
        Assert.NotNull(holder.Configuration);
        Assert.Equal("value1", holder.Configuration["key1"]);
    }

    [Fact]
    public void AdjustConfigManagerBuilder_CorrectlyReflectNewValues()
    {
        _decryptorMock.Setup(x => x.Decrypt("encrypted")).Returns("DECRYPTED");

        var manager = new ConfigurationManager();

        var valueProviderA = new Dictionary<string, string?>
        {
            { "value", "a" }
        };

        var encryption = new Dictionary<string, string?>
        {
            { "value", "{cipher}encrypted" }
        };

        manager.AddInMemoryCollection(valueProviderA);
        manager.AddInMemoryCollection(encryption);
        manager.AddEncryptionResolver(_decryptorMock.Object);
        string? result = manager.GetValue<string>("value");
        Assert.Equal("DECRYPTED", result);

        _decryptorMock.Verify(x => x.Decrypt("encrypted"));
        _decryptorMock.VerifyNoOtherCalls();
    }
}

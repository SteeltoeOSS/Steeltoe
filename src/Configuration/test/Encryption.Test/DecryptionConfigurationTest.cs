// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.Encryption.Cryptography;
using Steeltoe.Configuration.Placeholder;
using Xunit.Abstractions;

namespace Steeltoe.Configuration.Encryption.Test;

public sealed class DecryptionConfigurationTest : IDisposable
{
    private readonly LoggerFactory _loggerFactory;

    public DecryptionConfigurationTest(ITestOutputHelper testOutputHelper)
    {
        var loggerProvider = new XunitLoggerProvider(testOutputHelper);
        _loggerFactory = new LoggerFactory([loggerProvider]);
    }

    [Fact]
    public void Takes_ownership_of_existing_sources()
    {
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection();
        builder.AddInMemoryCollection();
        builder.AddDecryption(_loggerFactory);

        builder.Sources.Should().ContainSingle();
        DecryptionConfigurationSource decryptionSource = builder.Sources[0].Should().BeOfType<DecryptionConfigurationSource>().Subject;

        decryptionSource.Sources.Should().HaveCount(2);
        decryptionSource.Sources.Should().AllBeOfType<MemoryConfigurationSource>();
    }

    [Fact]
    public void Decrypts_encrypted_values()
    {
        var decryptor = new ToUpperCaseDecryptor();

        var appSettings = new Dictionary<string, string?>
        {
            ["key1"] = "value1",
            ["key2"] = "{cipher}example-cipher-without-alias",
            ["key3"] = "{cipher}{key:key-alias}example-cipher-with-alias"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        builder.AddDecryption(decryptor, _loggerFactory);
        IConfiguration configuration = builder.Build();

        configuration["no-key"].Should().BeNull();
        configuration["key1"].Should().Be("value1");
        configuration["key2"].Should().Be("EXAMPLE-CIPHER-WITHOUT-ALIAS");
        configuration["key3"].Should().Be("EXAMPLE-CIPHER-WITH-ALIAS|KEY-ALIAS");

        configuration["key2"] = "{cipher}{key:other-alias}other-cipher-with-alias";
        configuration["key2"].Should().Be("OTHER-CIPHER-WITH-ALIAS|OTHER-ALIAS");

        configuration["key2"] = "no-cipher";
        configuration["key2"].Should().Be("no-cipher");
    }

    [Fact]
    public void Can_resolve_placeholder_to_decrypted_value()
    {
        var decryptor = new ToUpperCaseDecryptor();

        var appSettings = new Dictionary<string, string?>
        {
            ["result"] = "start-${test-placeholder}-end",
            ["test-placeholder"] = "{cipher}secret"
        };

        var builder = new ConfigurationBuilder();
        builder.Sources.Clear();
        builder.AddInMemoryCollection(appSettings);
        builder.AddDecryption(decryptor, _loggerFactory);
        builder.AddPlaceholderResolver(_loggerFactory);
        IConfiguration configuration = builder.Build();

        configuration["result"].Should().Be("start-SECRET-end");
    }

    public void Dispose()
    {
        _loggerFactory.Dispose();
    }

    private sealed class ToUpperCaseDecryptor : ITextDecryptor
    {
        public string Decrypt(string fullCipher)
        {
            return fullCipher.ToUpperInvariant();
        }

        public string Decrypt(string fullCipher, string alias)
        {
            return $"{fullCipher.ToUpperInvariant()}|{alias.ToUpperInvariant()}";
        }
    }
}

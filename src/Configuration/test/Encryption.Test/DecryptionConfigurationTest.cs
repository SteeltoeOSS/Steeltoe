// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.Encryption.Cryptography;
using Steeltoe.Configuration.Placeholder;

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

        DecryptionConfigurationSource source = builder.Sources.Should().ContainSingle().Which.Should().BeOfType<DecryptionConfigurationSource>().Subject;

        source.Sources.Should().HaveCount(2);
        source.Sources.Should().AllBeOfType<MemoryConfigurationSource>();
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
        using ConfigurationRoot configurationRoot = builder.BuildAsRoot();

        configurationRoot["no-key"].Should().BeNull();
        configurationRoot["key1"].Should().Be("value1");
        configurationRoot["key2"].Should().Be("EXAMPLE-CIPHER-WITHOUT-ALIAS");
        configurationRoot["key3"].Should().Be("EXAMPLE-CIPHER-WITH-ALIAS|KEY-ALIAS");

        configurationRoot["key2"] = "{cipher}{key:other-alias}other-cipher-with-alias";
        configurationRoot["key2"].Should().Be("OTHER-CIPHER-WITH-ALIAS|OTHER-ALIAS");

        configurationRoot["key2"] = "no-cipher";
        configurationRoot["key2"].Should().Be("no-cipher");
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
        using ConfigurationRoot configurationRoot = builder.BuildAsRoot();

        configurationRoot["result"].Should().Be("start-SECRET-end");
    }

    [Fact]
    public void Decrypted_values_in_placeholder_resolution_are_only_logged_at_trace_level()
    {
        var decryptor = new ToUpperCaseDecryptor();
        using var capturingProvider = new CapturingLoggerProvider();
        using var loggerFactory = new LoggerFactory([capturingProvider]);

        var appSettings = new Dictionary<string, string?>
        {
            ["secret"] = "{cipher}classified",
            ["greeting"] = "hello-${secret}-world"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        builder.AddDecryption(decryptor, loggerFactory);
        builder.AddPlaceholderResolver(loggerFactory);
        using ConfigurationRoot configurationRoot = builder.BuildAsRoot();

        _ = configurationRoot["greeting"];

        IList<string> logLines = capturingProvider.GetAll();
        string[] sensitiveLines = [.. logLines.Where(message => message.Contains("CLASSIFIED", StringComparison.Ordinal))];

        sensitiveLines.Should().NotBeEmpty();
        sensitiveLines.Should().AllSatisfy(message => message.Should().StartWith("TRCE"));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Disposes_providers_of_host_and_app_configuration_when_host_is_disposed(bool usePlaceholder, bool useDecryption)
    {
        var hostSource = new DisposeCountingConfigurationSource();
        var appSource = new DisposeCountingConfigurationSource();

        // HostBuilder chains the host configuration into the app configuration, which then wraps it in the composite sources
        // that are added by AddDecryption() and AddPlaceholderResolver().
        HostBuilder builder = TestHostBuilderFactory.Create();
        builder.ConfigureHostConfiguration(configurationBuilder => configurationBuilder.Add(hostSource));

        builder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.Add(appSource);

            if (useDecryption)
            {
                configurationBuilder.AddDecryption(new ToUpperCaseDecryptor(), _loggerFactory);
            }

            if (usePlaceholder)
            {
                configurationBuilder.AddPlaceholderResolver(_loggerFactory);
            }
        });

        using (builder.Build())
        {
            hostSource.Providers.Should().ContainSingle().Which.DisposeCount.Should().Be(0);
            appSource.Providers.Should().ContainSingle().Which.DisposeCount.Should().Be(0);
        }

        hostSource.Providers.Should().ContainSingle().Which.DisposeCount.Should().Be(1);
        appSource.Providers.Should().ContainSingle().Which.DisposeCount.Should().Be(1);
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

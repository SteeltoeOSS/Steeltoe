// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Configuration.Encryption.Cryptography;

namespace Steeltoe.Configuration.Encryption.Test.Cryptography;

public sealed class TextDecryptorFactoryTest
{
    [Fact]
    public void Create_WhenDisabled_CreateNoneDecryptor()
    {
        var settings = new ConfigServerDecryptionSettings();

        TextDecryptorFactory.CreateDecryptor(settings).Should().BeOfType<NoneDecryptor>();
    }

    [Fact]
    public void Create_WhenEnabledWithKey_CreateAesDecryptor()
    {
        var settings = new ConfigServerDecryptionSettings
        {
            Enabled = true,
            Key = "something"
        };

        TextDecryptorFactory.CreateDecryptor(settings).Should().BeOfType<AesTextDecryptor>();
    }

    [Fact]
    public void Create_WhenEnabledKeyStoreLocation_CreateRsaDecryptor()
    {
        var settings = new ConfigServerDecryptionSettings
        {
            Enabled = true,
            KeyStore =
            {
                Location = "./Cryptography/server.jks",
                Password = "letmein",
                Alias = "mytestkey"
            }
        };

        TextDecryptorFactory.CreateDecryptor(settings).Should().BeOfType<RsaKeyStoreDecryptor>();
    }

    [Fact]
    public void Create_WhenEnabledInvalidStoreLocation_Throws()
    {
        var settings = new ConfigServerDecryptionSettings
        {
            Enabled = true,
            KeyStore =
            {
                Password = "letmein",
                Alias = "mytestkey"
            }
        };

        Action action = () => TextDecryptorFactory.CreateDecryptor(settings);

        action.Should().ThrowExactly<DecryptionException>();
    }

    [Fact]
    public void Create_WhenEnabledInvalidStorePassword_Throws()
    {
        var settings = new ConfigServerDecryptionSettings
        {
            Enabled = true,
            KeyStore =
            {
                Location = "./Cryptography/server.jks",
                Alias = "mytestkey"
            }
        };

        Action action = () => TextDecryptorFactory.CreateDecryptor(settings);

        action.Should().ThrowExactly<DecryptionException>();
    }

    [Fact]
    public void Create_WhenEnabledInvalidStoreAlias_Throws()
    {
        var settings = new ConfigServerDecryptionSettings
        {
            Enabled = true,
            KeyStore =
            {
                Location = "./Cryptography/server.jks",
                Password = "letmein"
            }
        };

        Action action = () => TextDecryptorFactory.CreateDecryptor(settings);

        action.Should().ThrowExactly<DecryptionException>();
    }

    [Fact]
    public void Create_WhenEnabledValidKeyAndStore_Throws()
    {
        var settings = new ConfigServerDecryptionSettings
        {
            Enabled = true,
            Key = "something",
            KeyStore =
            {
                Location = "./Cryptography/server.jks",
                Password = "letmein",
                Alias = "mytestkey"
            }
        };

        Action action = () => TextDecryptorFactory.CreateDecryptor(settings);

        action.Should().ThrowExactly<DecryptionException>();
    }

    [Fact]
    public void Create_WhenEnabledNothingConfigured_Throws()
    {
        var settings = new ConfigServerDecryptionSettings
        {
            Enabled = true
        };

        Action action = () => TextDecryptorFactory.CreateDecryptor(settings);

        action.Should().ThrowExactly<DecryptionException>();
    }
}

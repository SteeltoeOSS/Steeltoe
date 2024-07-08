// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Configuration.Encryption.Decryption;

namespace Steeltoe.Configuration.Encryption.Test.Decryption;

public sealed class EncryptionFactoryTest
{
    [Fact]
    public void Create_WhenDisabled_CreateNoopDecryptor()
    {
        var configServerEncryptionSettings = new ConfigServerEncryptionSettings();
        Assert.IsType<NoopDecryptor>(EncryptionFactory.CreateEncryptor(configServerEncryptionSettings));
    }

    [Fact]
    public void Create_WhenEnabledWithKey_CreateAesDecryptor()
    {
        var configServerEncryptionSettings = new ConfigServerEncryptionSettings
        {
            EncryptionEnabled = true,
            EncryptionKey = "something"
        };

        Assert.IsType<AesTextDecryptor>(EncryptionFactory.CreateEncryptor(configServerEncryptionSettings));
    }

    [Fact]
    public void Create_WhenEnabledKeyStoreLocation_CreateRsaDecryptor()
    {
        var configServerEncryptionSettings = new ConfigServerEncryptionSettings
        {
            EncryptionEnabled = true,
            KeyStore =
            {
                Location = "./Decryption/server.jks",
                Password = "letmein",
                Alias = "mytestkey"
            }
        };

        Assert.IsType<RsaKeyStoreDecryptor>(EncryptionFactory.CreateEncryptor(configServerEncryptionSettings));
    }

    [Fact]
    public void Create_WhenEnabledInvalidStoreLocation_Throws()
    {
        var configServerEncryptionSettings = new ConfigServerEncryptionSettings
        {
            EncryptionEnabled = true,
            KeyStore =
            {
                Password = "letmein",
                Alias = "mytestkey"
            }
        };

        Assert.Throws<DecryptionException>(() => EncryptionFactory.CreateEncryptor(configServerEncryptionSettings));
    }

    [Fact]
    public void Create_WhenEnabledInvalidStorePassword_Throws()
    {
        var configServerEncryptionSettings = new ConfigServerEncryptionSettings
        {
            EncryptionEnabled = true,
            KeyStore =
            {
                Location = "./Decryption/server.jks",
                Alias = "mytestkey"
            }
        };

        Assert.Throws<DecryptionException>(() => EncryptionFactory.CreateEncryptor(configServerEncryptionSettings));
    }

    [Fact]
    public void Create_WhenEnabledInvalidStoreAlias_Throws()
    {
        var configServerEncryptionSettings = new ConfigServerEncryptionSettings
        {
            EncryptionEnabled = true,
            KeyStore =
            {
                Location = "./Decryption/server.jks",
                Password = "letmein"
            }
        };

        Assert.Throws<DecryptionException>(() => EncryptionFactory.CreateEncryptor(configServerEncryptionSettings));
    }

    [Fact]
    public void Create_WhenEnabledValidKeyAndStore_Throws()
    {
        var configServerEncryptionSettings = new ConfigServerEncryptionSettings
        {
            EncryptionEnabled = true,
            EncryptionKey = "something",
            KeyStore =
            {
                Location = "./Decryption/server.jks",
                Password = "letmein",
                Alias = "mytestkey"
            }
        };

        Assert.Throws<DecryptionException>(() => EncryptionFactory.CreateEncryptor(configServerEncryptionSettings));
    }

    [Fact]
    public void Create_WhenEnabledNothingConfigured_Throws()
    {
        var configServerEncryptionSettings = new ConfigServerEncryptionSettings
        {
            EncryptionEnabled = true
        };

        Assert.Throws<DecryptionException>(() => EncryptionFactory.CreateEncryptor(configServerEncryptionSettings));
    }
}

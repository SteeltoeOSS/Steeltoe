// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Configuration.Encryption.ConfigServer.Test;

public class EncryptionFactoryTest
{
    [Fact]
    public void Create_WhenDisabled_CreateNoopDecrytor()
    {
        var configServerEncryptionSettings = new ConfigServerEncryptionSettings();
        Assert.IsType<NoopDecryptor>(EncryptionFactory.CreateEncryptor(configServerEncryptionSettings));
    }
    
    [Fact]
    public void Create_WhenEnabledWithKey_CreateAesDecrytor()
    {
        var configServerEncryptionSettings = new ConfigServerEncryptionSettings
        {
            EncryptionEnabled = true,
            EncryptionKey = "something"
        };
        Assert.IsType<AesTextDecryptor>(EncryptionFactory.CreateEncryptor(configServerEncryptionSettings));
    }
    
    [Fact]
    public void Create_WhenEnabledKeyStorelocation_CreateRsaDecrytor()
    {
        var configServerEncryptionSettings = new ConfigServerEncryptionSettings
        {
            EncryptionEnabled = true,
            EncryptionKeyStoreLocation = "./server.jks",
            EncryptionKeyStorePassword = "letmein",
            EncryptionKeyStoreAlias = "mytestkey"
        };
        Assert.IsType<RsaKeyStoreDecryptor>(EncryptionFactory.CreateEncryptor(configServerEncryptionSettings));
    }
}

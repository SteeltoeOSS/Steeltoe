// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Configuration.Encryption.Decryption;

internal static class ConfigurationSettingsHelper
{
    private const string ConfigurationPrefix = "encrypt";

    public static void Initialize(ConfigServerEncryptionSettings settings, IConfiguration configuration)
    {
        ArgumentGuard.NotNull(settings);
        ArgumentGuard.NotNull(configuration);

        IConfigurationSection configurationSection = configuration.GetSection(ConfigurationPrefix);

        settings.EncryptionEnabled = configurationSection.GetValue("enabled", settings.EncryptionEnabled);
        settings.Rsa.Strong = configurationSection.GetValue("rsa:strong", settings.Rsa.Strong);
        settings.Rsa.Salt = configurationSection.GetValue("rsa:salt", settings.Rsa.Salt);
        settings.Rsa.Algorithm = configurationSection.GetValue("rsa:algorithm", settings.Rsa.Algorithm);
        settings.KeyStore.Location = configurationSection.GetValue("keyStore:location", settings.KeyStore.Location);
        settings.KeyStore.Password = configurationSection.GetValue("keyStore:password", settings.KeyStore.Password);
        settings.KeyStore.Alias = configurationSection.GetValue("keyStore:alias", settings.KeyStore.Alias);
        settings.EncryptionKey = configurationSection.GetValue("key", settings.EncryptionKey);
    }
}

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
        settings.EncryptionRsaStrong = configurationSection.GetValue("rsa:strong", settings.EncryptionRsaStrong);
        settings.EncryptionRsaSalt = configurationSection.GetValue("rsa:salt", settings.EncryptionRsaSalt);
        settings.EncryptionRsaAlgorithm = configurationSection.GetValue("rsa:algorithm", settings.EncryptionRsaAlgorithm);
        settings.EncryptionKeyStoreLocation = configurationSection.GetValue("keyStore:location", settings.EncryptionKeyStoreLocation);
        settings.EncryptionKeyStorePassword = configurationSection.GetValue("keyStore:password", settings.EncryptionKeyStorePassword);
        settings.EncryptionKeyStoreAlias = configurationSection.GetValue("keyStore:alias", settings.EncryptionKeyStoreAlias);
        settings.EncryptionKey = configurationSection.GetValue("key", settings.EncryptionKey);
    }
}

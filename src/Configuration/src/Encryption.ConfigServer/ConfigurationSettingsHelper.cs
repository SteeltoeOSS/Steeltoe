// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Configuration.Encryption.ConfigServer;

internal static class ConfigurationSettingsHelper
{
    private const string ConfigurationPrefix = "spring:cloud:config";

    public static void Initialize(ConfigServerEncryptionSettings settings, IConfiguration configuration)
    {
        ArgumentGuard.NotNull(settings);
        ArgumentGuard.NotNull(configuration);

        IConfigurationSection configurationSection = configuration.GetSection(ConfigurationPrefix);

        settings.EncryptionEnabled = configurationSection.GetValue("encrypt:enabled", settings.EncryptionEnabled);
        settings.EncryptionRsaStrong = configurationSection.GetValue("encrypt:rsa:strong", settings.EncryptionRsaStrong);
        settings.EncryptionRsaSalt = configurationSection.GetValue("encrypt:rsa:salt", settings.EncryptionRsaSalt);
        settings.EncryptionRsaAlgorithm = configurationSection.GetValue("encrypt:rsa:algorithm", settings.EncryptionRsaAlgorithm);
        settings.EncryptionKeyStoreLocation = configurationSection.GetValue("encrypt:keyStore:location", settings.EncryptionKeyStoreLocation);
        settings.EncryptionKeyStorePassword = configurationSection.GetValue("encrypt:keyStore:password", settings.EncryptionKeyStorePassword);
        settings.EncryptionKeyStoreAlias = configurationSection.GetValue("encrypt:keyStore:alias", settings.EncryptionKeyStoreAlias);
        settings.EncryptionKey = configurationSection.GetValue("encrypt:key", settings.EncryptionKey);
    }
}

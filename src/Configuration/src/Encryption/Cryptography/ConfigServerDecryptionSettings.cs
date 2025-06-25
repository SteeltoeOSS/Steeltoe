// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.Encryption.Cryptography;

/// <summary>
/// Holds settings used to configure decryption for the Spring Cloud Config Server provider.
/// </summary>
internal sealed class ConfigServerDecryptionSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether decryption is enabled. Default value: false.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets the settings related to RSA cryptography.
    /// </summary>
    [ConfigurationKeyName("RSA")]
    public RsaCryptoSettings Rsa { get; } = new();

    /// <summary>
    /// Gets the settings related to the key store.
    /// </summary>
    public CryptoKeyStoreSettings KeyStore { get; } = new();

    /// <summary>
    /// Gets or sets the symmetric cryptographic key.
    /// </summary>
    public string? Key { get; set; }

    public static ITextDecryptor CreateTextDecryptor(IConfiguration configuration)
    {
        var settings = new ConfigServerDecryptionSettings();
        ConfigurationSettingsBinder.Initialize(settings, configuration);

        return TextDecryptorFactory.CreateDecryptor(settings);
    }
}

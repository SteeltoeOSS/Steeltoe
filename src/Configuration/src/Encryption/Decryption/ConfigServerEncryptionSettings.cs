// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.Encryption.Decryption;

/// <summary>
/// Holds settings used to configure encryption for the Spring Cloud Config Server provider.
/// </summary>
internal sealed class ConfigServerEncryptionSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether encryption is enabled.
    /// </summary>
    public bool EncryptionEnabled { get; set; }

    /// <summary>
    /// Gets the settings related to RSA encryption.
    /// </summary>
    [ConfigurationKeyName("RSA")]
    public RsaEncryptionSettings Rsa { get; } = new();

    /// <summary>
    /// Gets the settings related to the key store.
    /// </summary>
    public EncryptionKeyStoreSettings KeyStore { get; } = new();

    /// <summary>
    /// Gets or sets the key of the simple encryption.
    /// </summary>
    public string? EncryptionKey { get; set; }

    internal static ITextDecryptor CreateTextDecryptor(IConfiguration configuration)
    {
        var settings = new ConfigServerEncryptionSettings();
        ConfigurationSettingsHelper.Initialize(settings, configuration);

        return EncryptionFactory.CreateEncryptor(settings);
    }
}

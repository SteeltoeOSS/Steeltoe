// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.Encryption.Decryption;

/// <summary>
/// Holds the settings used to configure the Spring Cloud Config Server provider <see cref="ConfigServerConfigurationProvider" />.
/// </summary>
public sealed class ConfigServerEncryptionSettings
{
    /// <summary>
    /// Default Encryption method.
    /// </summary>
    public const string DefaultEncryptionRsaAlgorithm = "DEFAULT";

    /// <summary>
    /// Default salt.
    /// </summary>
    public const string DefaultEncryptionRsaSalt = "deadbeef";

    /// <summary>
    /// Gets or sets a value indicating whether decryption is enabled.
    /// </summary>
    public bool EncryptionEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether strong encryption flag is enabled.
    /// </summary>
    public bool EncryptionRsaStrong { get; set; }

    /// <summary>
    /// Gets or sets the salt value.
    /// </summary>
    public string EncryptionRsaSalt { get; set; }

    /// <summary>
    /// Gets or sets the Rsa Algorithm (DEFAULT or OAEP).
    /// </summary>
    public string EncryptionRsaAlgorithm { get; set; }

    /// <summary>
    /// Gets or sets the location of the keystore.
    /// </summary>
    public string EncryptionKeyStoreLocation { get; set; }

    /// <summary>
    /// Gets or sets the keystore password.
    /// </summary>
    public string EncryptionKeyStorePassword { get; set; }

    /// <summary>
    /// Gets or sets the alias of the key in the keystore.
    /// </summary>
    public string EncryptionKeyStoreAlias { get; set; }

    /// <summary>
    /// Gets or sets the key of the simple encryption.
    /// </summary>
    public string EncryptionKey { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerEncryptionSettings" /> class.
    /// </summary>
    /// <remarks>
    /// Initializes the Config Server client settings with defaults.
    /// </remarks>
    public ConfigServerEncryptionSettings()
    {
        EncryptionRsaAlgorithm = DefaultEncryptionRsaAlgorithm;
        EncryptionRsaSalt = DefaultEncryptionRsaSalt;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.Encryption.Decryption;

/// <summary>
/// Holds settings related to an encryption key store.
/// </summary>
internal sealed class EncryptionKeyStoreSettings
{
    /// <summary>
    /// Gets or sets the location of the keystore.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets the keystore password.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the alias of the key in the keystore.
    /// </summary>
    public string? Alias { get; set; }
}

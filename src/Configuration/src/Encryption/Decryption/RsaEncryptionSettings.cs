// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.Encryption.Decryption;

/// <summary>
/// Holds settings related to RSA encryption.
/// </summary>
internal sealed class RsaEncryptionSettings
{
    /// <summary>
    /// Default Encryption method.
    /// </summary>
    internal const string DefaultAlgorithm = "DEFAULT";

    /// <summary>
    /// Default salt.
    /// </summary>
    internal const string DefaultSalt = "deadbeef";

    /// <summary>
    /// Gets or sets the RSA algorithm (DEFAULT or OAEP). Default value: DEFAULT.
    /// </summary>
    public string? Algorithm { get; set; } = DefaultAlgorithm;

    /// <summary>
    /// Gets or sets the salt value. Default value: deadbeef.
    /// </summary>
    public string? Salt { get; set; } = DefaultSalt;

    /// <summary>
    /// Gets or sets a value indicating whether strong encryption is enabled. Default value: false.
    /// </summary>
    public bool Strong { get; set; }
}

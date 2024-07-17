// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;

namespace Steeltoe.Common.Certificates;

/// <summary>
/// Indicates where to load a <see cref="X509Certificate2" /> from.
/// </summary>
internal sealed class CertificateSettings
{
    /// <summary>
    /// Gets or sets the local path to a certificate file on disk. Use <see cref="PrivateKeyFilePath" /> if the private key is stored in another file.
    /// </summary>
    public string? CertificateFilePath { get; set; }

    /// <summary>
    /// Gets or sets the local path to a private key file on disk (optional).
    /// </summary>
    public string? PrivateKeyFilePath { get; set; }
}

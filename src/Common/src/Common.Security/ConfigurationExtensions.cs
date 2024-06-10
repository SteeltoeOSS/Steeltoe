// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Configuration;

namespace Steeltoe.Common.Security;

internal static class ConfigurationExtensions
{
    /// <summary>
    /// Adds file path information for a certificate and (optional) private key to configuration, for use with <see cref="CertificateOptions" />.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="IConfigurationBuilder" />.
    /// </param>
    /// <param name="certificateName">
    /// Name of the certificate, or <see cref="string.Empty" /> for an unnamed certificate.
    /// </param>
    /// <param name="certificateFilePath">
    /// The path on disk to locate a valid certificate file.
    /// </param>
    /// <param name="privateKeyFilePath">
    /// The path on disk to locate a valid PEM-encoded RSA key file.
    /// </param>
    internal static IConfigurationBuilder AddCertificate(this IConfigurationBuilder builder, string certificateName, string certificateFilePath,
        string? privateKeyFilePath = null)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNullOrEmpty(certificateFilePath);

        string keyPrefix = string.IsNullOrEmpty(certificateName)
            ? $"{CertificateOptions.ConfigurationKeyPrefix}{ConfigurationPath.KeyDelimiter}"
            : $"{CertificateOptions.ConfigurationKeyPrefix}{ConfigurationPath.KeyDelimiter}{certificateName}{ConfigurationPath.KeyDelimiter}";

        var keys = new Dictionary<string, string?>
        {
            { $"{keyPrefix}CertificateFilePath", certificateFilePath }
        };

        if (!string.IsNullOrEmpty(privateKeyFilePath))
        {
            keys[$"{keyPrefix}PrivateKeyFilePath"] = privateKeyFilePath;
        }

        builder.AddInMemoryCollection(keys);
        return builder;
    }
}

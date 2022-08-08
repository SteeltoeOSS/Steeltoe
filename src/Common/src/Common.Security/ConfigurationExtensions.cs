// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Options;

namespace Steeltoe.Common.Security;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds the contents of pem encoded certificate and key files to configuration, for use with <see cref="CertificateOptions" />.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="IConfigurationBuilder" />.
    /// </param>
    /// <param name="certFilePath">
    /// The path on disk to locate a valid pem-encoded certificate file.
    /// </param>
    /// <param name="keyFilePath">
    /// The path on disk to locate a valid pem-encoded RSA key.
    /// </param>
    /// <param name="optional">
    /// Whether or not to throw an exception if the files aren't found.
    /// </param>
    public static IConfigurationBuilder AddPemFiles(this IConfigurationBuilder builder, string certFilePath, string keyFilePath, bool optional = false)
    {
        ArgumentGuard.NotNull(builder);

        if (string.IsNullOrEmpty(certFilePath))
        {
            throw new ArgumentException(nameof(certFilePath));
        }

        if (string.IsNullOrEmpty(keyFilePath))
        {
            throw new ArgumentException(nameof(keyFilePath));
        }

        if (optional && (!File.Exists(certFilePath) || !File.Exists(keyFilePath)))
        {
            return builder;
        }

        builder.Add(new PemCertificateSource(certFilePath, keyFilePath));
        return builder;
    }

    /// <summary>
    /// Adds information on a certificate file to configuration, for use with <see cref="CertificateOptions" />.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="IConfigurationBuilder" />.
    /// </param>
    /// <param name="certFilePath">
    /// The path on disk to locate a valid certificate file.
    /// </param>
    /// <param name="optional">
    /// Whether or not to throw an exception if the file isn't found.
    /// </param>
    /// <remarks>
    /// In contrast with <see cref="AddPemFiles(IConfigurationBuilder, string, string, bool)" />, this extension adds the path of the file instead of the
    /// contents. Certificate parsing is handled by <see cref="ConfigureCertificateOptions" />.
    /// </remarks>
    public static IConfigurationBuilder AddCertificateFile(this IConfigurationBuilder builder, string certFilePath, bool optional = false)
    {
        ArgumentGuard.NotNull(builder);

        if (string.IsNullOrEmpty(certFilePath))
        {
            throw new ArgumentException(nameof(certFilePath));
        }

        if (optional && !File.Exists(certFilePath))
        {
            return builder;
        }

        builder.Add(new CertificateSource(certFilePath));
        return builder;
    }
}

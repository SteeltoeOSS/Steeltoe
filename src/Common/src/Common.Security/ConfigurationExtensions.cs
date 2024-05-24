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
    /// <param name="certificateName">Name of the certificate.</param>
    /// <param name="certificateFilePath">
    /// The path on disk to locate a valid pem-encoded certificate file.
    /// </param>
    /// <param name="keyFilePath">
    /// The path on disk to locate a valid pem-encoded RSA key.
    /// </param>
    /// <param name="optional">
    /// Whether to throw an exception if the files aren't found.
    /// </param>
    public static IConfigurationBuilder AddPemFiles(this IConfigurationBuilder builder, string certificateName, string certificateFilePath, string keyFilePath, bool optional = false)
    {
        ArgumentGuard.NotNull(builder);

        ArgumentGuard.NotNullOrEmpty(certificateName);
        ArgumentGuard.NotNullOrEmpty(certificateFilePath);
        ArgumentGuard.NotNullOrEmpty(keyFilePath);

        if (optional && (!File.Exists(certificateFilePath) || !File.Exists(keyFilePath)))
        {
            return builder;
        }

        builder.Add(new PemCertificateSource(certificateName, certificateFilePath, keyFilePath));
        return builder;
    }

    /// <summary>
    /// Adds information on a certificate file to configuration, for use with <see cref="CertificateOptions" />.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="IConfigurationBuilder" />.
    /// </param>
    /// <param name="certificateName">Name of the certificate.</param>
    /// <param name="certificateFilePath">
    /// The path on disk to locate a valid certificate file.
    /// </param>
    /// <param name="optional">
    /// Whether to throw an exception if the file isn't found.
    /// </param>
    /// <remarks>
    /// In contrast with <see cref="AddPemFiles(IConfigurationBuilder, string, string, string, bool)" />, this extension adds the path of the file instead of the
    /// contents. Certificate parsing is handled by <see cref="ConfigureNamedCertificateOptions" />.
    /// </remarks>
    public static IConfigurationBuilder AddCertificateFile(this IConfigurationBuilder builder, string certificateName, string certificateFilePath, bool optional = false)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNullOrEmpty(certificateFilePath);

        if (optional && !File.Exists(certificateFilePath))
        {
            return builder;
        }

        builder.Add(new CertificateSource(certificateName, certificateFilePath));
        return builder;
    }
}

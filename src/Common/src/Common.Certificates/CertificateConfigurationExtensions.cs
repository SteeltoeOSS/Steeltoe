// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Configuration;

namespace Steeltoe.Common.Certificates;

public static class CertificateConfigurationExtensions
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

    /// <summary>
    /// Adds PEM certificate files representing application identity to the application configuration. When running outside of Cloud Foundry-based platforms,
    /// this method will create certificates resembling those found on the platform.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="IConfigurationBuilder" />.
    /// </param>
    /// <remarks>
    /// When running outside of Cloud Foundry, the CA and Intermediate certificates will be created in a directory above the current project, so that they
    /// can be shared between different projects in the same solution.
    /// </remarks>
    public static IConfigurationBuilder AddAppInstanceIdentityCertificate(this IConfigurationBuilder builder)
    {
        return AddAppInstanceIdentityCertificate(builder, null, null);
    }

    /// <summary>
    /// Adds PEM certificate files representing application identity to the application configuration. When running outside of Cloud Foundry-based platforms,
    /// this method will create certificates resembling those found on the platform.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="IConfigurationBuilder" />.
    /// </param>
    /// <param name="organizationId">
    /// (Optional) A GUID representing an organization, for use with Cloud Foundry certificate-based authorization policy.
    /// </param>
    /// <param name="spaceId">
    /// (Optional) A GUID representing a space, for use with Cloud Foundry certificate-based authorization policy.
    /// </param>
    /// <remarks>
    /// When running outside of Cloud Foundry, the CA and Intermediate certificates will be created in a directory above the current project, so that they
    /// can be shared between different projects in the same solution.
    /// </remarks>
    public static IConfigurationBuilder AddAppInstanceIdentityCertificate(this IConfigurationBuilder builder, Guid? organizationId, Guid? spaceId)
    {
        ArgumentGuard.NotNull(builder);

        if (!Platform.IsCloudFoundry)
        {
            organizationId ??= Guid.NewGuid();
            spaceId ??= Guid.NewGuid();

            var writer = new LocalCertificateWriter();
            writer.Write(organizationId.Value, spaceId.Value);

            Environment.SetEnvironmentVariable("CF_SYSTEM_CERT_PATH",
                Path.Combine(Directory.GetParent(LocalCertificateWriter.AppBasePath)!.ToString(), LocalCertificateWriter.CertificateDirectoryName));

            Environment.SetEnvironmentVariable("CF_INSTANCE_CERT",
                Path.Combine(LocalCertificateWriter.AppBasePath, LocalCertificateWriter.CertificateDirectoryName,
                    $"{writer.CertificateFilenamePrefix}Cert.pem"));

            Environment.SetEnvironmentVariable("CF_INSTANCE_KEY",
                Path.Combine(LocalCertificateWriter.AppBasePath, LocalCertificateWriter.CertificateDirectoryName,
                    $"{writer.CertificateFilenamePrefix}Key.pem"));
        }

        string? certificateFile = Environment.GetEnvironmentVariable("CF_INSTANCE_CERT");
        string? keyFile = Environment.GetEnvironmentVariable("CF_INSTANCE_KEY");

        if (certificateFile != null && keyFile != null)
        {
            builder.AddCertificate("AppInstanceIdentity", certificateFile, keyFile);
        }

        return builder;
    }
}

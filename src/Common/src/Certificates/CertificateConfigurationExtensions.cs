// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Common.Certificates;

public static class CertificateConfigurationExtensions
{
    internal const string AppInstanceIdentityCertificateName = "AppInstanceIdentity";

    /// <summary>
    /// Adds PEM certificate files representing application identity to the application configuration. When running outside of Cloud Foundry-based platforms,
    /// this method will create certificates resembling those found on the platform.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <remarks>
    /// When running outside of Cloud Foundry, the CA and Intermediate certificates will be created in a directory above the current project, so that they
    /// can be shared between different projects in the same solution.
    /// </remarks>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder AddAppInstanceIdentityCertificate(this IConfigurationBuilder builder)
    {
        return AddAppInstanceIdentityCertificate(builder, TimeProvider.System, null, null);
    }

    /// <summary>
    /// Adds PEM certificate files representing application identity to the application configuration. When running outside of Cloud Foundry-based platforms,
    /// this method will create certificates resembling those found on the platform.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <param name="orgId">
    /// (Optional) A GUID representing an organization (org), for use with Cloud Foundry certificate-based authorization policy.
    /// </param>
    /// <param name="spaceId">
    /// (Optional) A GUID representing a space, for use with Cloud Foundry certificate-based authorization policy.
    /// </param>
    /// <remarks>
    /// When running outside of Cloud Foundry, the CA and Intermediate certificates will be created in a directory above the current project, so that they
    /// can be shared between different projects in the same solution.
    /// </remarks>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder AddAppInstanceIdentityCertificate(this IConfigurationBuilder builder, Guid? orgId, Guid? spaceId)
    {
        return AddAppInstanceIdentityCertificate(builder, TimeProvider.System, orgId, spaceId);
    }

    /// <summary>
    /// Adds PEM certificate files representing application identity to the application configuration. When running outside of Cloud Foundry-based platforms,
    /// this method will create certificates resembling those found on the platform.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <param name="timeProvider">
    /// Provides access to the system time.
    /// </param>
    /// <param name="orgId">
    /// (Optional) A GUID representing an organization (org), for use with Cloud Foundry certificate-based authorization policy.
    /// </param>
    /// <param name="spaceId">
    /// (Optional) A GUID representing a space, for use with Cloud Foundry certificate-based authorization policy.
    /// </param>
    /// <remarks>
    /// When running outside of Cloud Foundry, the CA and Intermediate certificates will be created in a directory above the current project, so that they
    /// can be shared between different projects in the same solution.
    /// </remarks>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder AddAppInstanceIdentityCertificate(this IConfigurationBuilder builder, TimeProvider timeProvider, Guid? orgId,
        Guid? spaceId)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (!Platform.IsCloudFoundry)
        {
            orgId ??= Guid.NewGuid();
            spaceId ??= Guid.NewGuid();

            var writer = new LocalCertificateWriter(timeProvider);
            writer.Write(orgId.Value, spaceId.Value);

            Environment.SetEnvironmentVariable("CF_SYSTEM_CERT_PATH",
                Path.Combine(Directory.GetParent(LocalCertificateWriter.AppBasePath)?.FullName ?? string.Empty,
                    LocalCertificateWriter.CertificateDirectoryName));

            Environment.SetEnvironmentVariable("CF_INSTANCE_CERT",
                Path.Combine(LocalCertificateWriter.AppBasePath, LocalCertificateWriter.CertificateDirectoryName,
                    $"{LocalCertificateWriter.CertificateFilenamePrefix}Cert.pem"));

            Environment.SetEnvironmentVariable("CF_INSTANCE_KEY",
                Path.Combine(LocalCertificateWriter.AppBasePath, LocalCertificateWriter.CertificateDirectoryName,
                    $"{LocalCertificateWriter.CertificateFilenamePrefix}Key.pem"));
        }

        string? certificateFile = Environment.GetEnvironmentVariable("CF_INSTANCE_CERT");
        string? keyFile = Environment.GetEnvironmentVariable("CF_INSTANCE_KEY");

        if (certificateFile != null && keyFile != null)
        {
            const string keyPrefix = $"{CertificateOptions.ConfigurationKeyPrefix}:{AppInstanceIdentityCertificateName}:";

            var keys = new Dictionary<string, string?>
            {
                [$"{keyPrefix}{nameof(CertificateSettings.CertificateFilePath)}"] = certificateFile,
                [$"{keyPrefix}{nameof(CertificateSettings.PrivateKeyFilePath)}"] = keyFile
            };

            builder.AddInMemoryCollection(keys);
        }

        return builder;
    }
}

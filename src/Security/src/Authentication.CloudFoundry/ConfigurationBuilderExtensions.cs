// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Common.Security;

namespace Steeltoe.Security.Authentication.CloudFoundry;

public static class ConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds PEM files representing application identity to application configuration. When running outside Pivotal Platform, will create certificates
    /// resembling those found on the platform.
    /// </summary>
    /// <param name="builder">
    /// Your <see cref="IConfigurationBuilder" />.
    /// </param>
    /// <param name="orgId">
    /// (Optional) A GUID representing an organization, for use with <see cref="CloudFoundryDefaults.SameOrganizationAuthorizationPolicy" /> authorization
    /// policy.
    /// </param>
    /// <param name="spaceId">
    /// (Optional) A GUID representing a space, for use with <see cref="CloudFoundryDefaults.SameSpaceAuthorizationPolicy" /> authorization policy.
    /// </param>
    public static IConfigurationBuilder AddCloudFoundryContainerIdentity(this IConfigurationBuilder builder, string orgId = null, string spaceId = null)
    {
        if (!Platform.IsCloudFoundry)
        {
            Guid orgGuid = orgId != null ? new Guid(orgId) : Guid.NewGuid();
            Guid spaceGuid = spaceId != null ? new Guid(spaceId) : Guid.NewGuid();

            var task = new LocalCertificateWriter();
            task.Write(orgGuid, spaceGuid);

            Environment.SetEnvironmentVariable("CF_INSTANCE_CERT",
                Path.Combine(LocalCertificateWriter.AppBasePath, "GeneratedCertificates", "SteeltoeInstanceCert.pem"));

            Environment.SetEnvironmentVariable("CF_INSTANCE_KEY",
                Path.Combine(LocalCertificateWriter.AppBasePath, "GeneratedCertificates", "SteeltoeInstanceKey.pem"));
        }

        string certFile = Environment.GetEnvironmentVariable("CF_INSTANCE_CERT");
        string keyFile = Environment.GetEnvironmentVariable("CF_INSTANCE_KEY");

        if (certFile == null || keyFile == null)
        {
            return builder;
        }

        return builder.AddCertificate("AppInstanceIdentity", certFile, keyFile);
    }
}

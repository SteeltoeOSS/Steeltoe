// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Common.Security;
using System;
using System.IO;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public static class ConfigurationBuilderExtensions
    {
        /// <summary>
        /// Adds PEM files representing application identity to application configuration.
        /// When running outside Pivotal Platform, will create certificates resembling those found on the platform.
        /// </summary>
        /// <param name="builder">Your <see cref="IConfigurationBuilder"/></param>
        /// <param name="orgId">(Optional) A GUID representing an organization, for use with <see cref="CloudFoundryDefaults.SameOrganizationAuthorizationPolicy"/> authorization policy</param>
        /// <param name="spaceId">(Optional) A GUID representing a space, for use with <see cref="CloudFoundryDefaults.SameSpaceAuthorizationPolicy"/> authorization policy</param>
        public static IConfigurationBuilder AddCloudFoundryContainerIdentity(this IConfigurationBuilder builder, string orgId = null, string spaceId = null)
        {
            if (!Platform.IsCloudFoundry)
            {
                var orgGuid = orgId != null ? new Guid(orgId) : Guid.NewGuid();
                var spaceGuid = spaceId != null ? new Guid(spaceId) : Guid.NewGuid();

                var task = new LocalCertificateWriter();
                task.Write(orgGuid, spaceGuid);

                Environment.SetEnvironmentVariable("CF_INSTANCE_CERT", Path.Combine(LocalCertificateWriter.AppBasePath, "GeneratedCertificates", "SteeltoeInstanceCert.pem"));
                Environment.SetEnvironmentVariable("CF_INSTANCE_KEY", Path.Combine(LocalCertificateWriter.AppBasePath, "GeneratedCertificates", "SteeltoeInstanceKey.pem"));
            }

            var certFile = Environment.GetEnvironmentVariable("CF_INSTANCE_CERT");
            var keyFile = Environment.GetEnvironmentVariable("CF_INSTANCE_KEY");

            if (certFile == null || keyFile == null)
            {
                return builder;
            }

            return builder.AddPemFiles(certFile, keyFile);
        }
    }
}
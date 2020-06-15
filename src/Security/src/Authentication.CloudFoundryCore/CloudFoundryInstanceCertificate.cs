// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace Steeltoe.Security.Authentication.Mtls
{
    public class CloudFoundryInstanceCertificate
    {
        // This pattern is found on certificates issued by Diego
        private const string CloudFoundryInstanceCertSubjectRegex =
            @"^CN=(?<instance>[0-9a-f-]+),\sOU=organization:(?<org>[0-9a-f-]+)\s\+\sOU=space:(?<space>[0-9a-f-]+)\s\+\sOU=app:(?<app>[0-9a-f-]+)$";

        // This pattern is found on certificates created in .NET
        private const string ValidInstanceCertSubjectRegex =
            @"^CN=(?<instance>[0-9a-f-]+),\sOU=app:(?<app>[0-9a-f-]+)\s\+\sOU=space:(?<space>[0-9a-f-]+)\s\+\sOU=organization:(?<org>[0-9a-f-]+)$";

        public static bool TryParse(X509Certificate2 certificate, out CloudFoundryInstanceCertificate cloudFoundryInstanceCertificate, ILogger logger = null)
        {
            cloudFoundryInstanceCertificate = null;
            if (certificate == null)
            {
                return false;
            }

            var cfInstanceMatch = Regex.Match(certificate.Subject.Replace("\"", string.Empty), CloudFoundryInstanceCertSubjectRegex);

            if (!cfInstanceMatch.Success)
            {
                cfInstanceMatch = Regex.Match(certificate.Subject.Replace("\"", string.Empty), ValidInstanceCertSubjectRegex);
            }

            if (cfInstanceMatch.Success)
            {
                cloudFoundryInstanceCertificate = new CloudFoundryInstanceCertificate
                {
                    OrgId = cfInstanceMatch.Groups["org"].Value,
                    SpaceId = cfInstanceMatch.Groups["space"].Value,
                    AppId = cfInstanceMatch.Groups["app"].Value,
                    InstanceId = cfInstanceMatch.Groups["instance"].Value,
                    Certificate = certificate
                };
            }
            else
            {
                logger?.LogWarning("Identity certificate did not match an expected pattern! Subject was: {0}", certificate.Subject);
            }

            return cfInstanceMatch.Success;
        }

        public string OrgId { get; private set; }

        public string SpaceId { get; private set; }

        public string AppId { get; private set; }

        public string InstanceId { get; private set; }

        public X509Certificate2 Certificate { get; private set; }
    }
}
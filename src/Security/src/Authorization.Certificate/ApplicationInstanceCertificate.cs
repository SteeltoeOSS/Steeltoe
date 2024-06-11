// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Steeltoe.Security.Authorization.Certificate;

internal sealed class ApplicationInstanceCertificate
{
    // This pattern is found on certificates issued by Diego
    private const string CloudFoundryInstanceCertificateSubjectRegex =
        @"^CN=(?<instance>[0-9a-f-]+),\sOU=organization:(?<org>[0-9a-f-]+)\s\+\sOU=space:(?<space>[0-9a-f-]+)\s\+\sOU=app:(?<app>[0-9a-f-]+)$";

    // This pattern is found on certificates created in Steeltoe
    private const string SteeltoeInstanceCertificateSubjectRegex =
        @"^CN=(?<instance>[0-9a-f-]+),\sOU=app:(?<app>[0-9a-f-]+)\s\+\sOU=space:(?<space>[0-9a-f-]+)\s\+\sOU=organization:(?<org>[0-9a-f-]+)$";

    public string OrganizationId { get; private set; }

    public string SpaceId { get; private set; }

    public string ApplicationId { get; private set; }

    public string InstanceId { get; private set; }

    private ApplicationInstanceCertificate(string organizationId, string spaceId, string applicationId, string instanceId)
    {
        OrganizationId = organizationId;
        SpaceId = spaceId;
        ApplicationId = applicationId;
        InstanceId = instanceId;
    }

    public static bool TryParse(X509Certificate2 certificate, [MaybeNullWhen(false)] out ApplicationInstanceCertificate outInstanceCertificate, ILogger logger)
    {
        outInstanceCertificate = null;

        Match instanceMatch = Regex.Match(certificate.Subject.Replace("\"", string.Empty, StringComparison.Ordinal),
            CloudFoundryInstanceCertificateSubjectRegex);

        if (!instanceMatch.Success)
        {
            instanceMatch = Regex.Match(certificate.Subject.Replace("\"", string.Empty, StringComparison.Ordinal), SteeltoeInstanceCertificateSubjectRegex);
        }

        if (instanceMatch.Success)
        {
            outInstanceCertificate = new ApplicationInstanceCertificate(instanceMatch.Groups["org"].Value, instanceMatch.Groups["space"].Value,
                instanceMatch.Groups["app"].Value, instanceMatch.Groups["instance"].Value);

            logger.LogTrace("Successfully parsed an identity certificate with subject {CertificateSubject}", certificate.Subject);
        }
        else
        {
            logger.LogError("Identity certificate did not match an expected pattern. Subject was: {CertificateSubject}", certificate.Subject);
        }

        return instanceMatch.Success;
    }
}

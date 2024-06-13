// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace Steeltoe.Security.Authorization.Certificate;

internal sealed class ApplicationInstanceCertificate
{
    // This pattern is found on certificates issued by Diego
    private static readonly Regex CloudFoundryInstanceCertificateSubjectRegex =
        new(@"^CN=(?<instance>[0-9a-f-]+),\sOU=organization:(?<org>[0-9a-f-]+)\s\+\sOU=space:(?<space>[0-9a-f-]+)\s\+\sOU=app:(?<app>[0-9a-f-]+)$",
            RegexOptions.Compiled | RegexOptions.Singleline, TimeSpan.FromSeconds(1));

    // This pattern is found on certificates created by Steeltoe
    private static readonly Regex SteeltoeInstanceCertificateSubjectRegex =
        new(@"^CN=(?<instance>[0-9a-f-]+),\sOU=app:(?<app>[0-9a-f-]+)\s\+\sOU=space:(?<space>[0-9a-f-]+)\s\+\sOU=organization:(?<org>[0-9a-f-]+)$",
            RegexOptions.Compiled | RegexOptions.Singleline, TimeSpan.FromSeconds(1));

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

    public static bool TryParse(X509Certificate2 certificate, [NotNullWhen(true)] out ApplicationInstanceCertificate? instanceCertificate)
    {
        instanceCertificate = null;

        Match instanceMatch = CloudFoundryInstanceCertificateSubjectRegex.Match(certificate.Subject);

        if (!instanceMatch.Success)
        {
            instanceMatch = SteeltoeInstanceCertificateSubjectRegex.Match(certificate.Subject);
        }

        if (instanceMatch.Success)
        {
            instanceCertificate = new ApplicationInstanceCertificate(instanceMatch.Groups["org"].Value, instanceMatch.Groups["space"].Value,
                instanceMatch.Groups["app"].Value, instanceMatch.Groups["instance"].Value);
        }

        return instanceMatch.Success;
    }
}

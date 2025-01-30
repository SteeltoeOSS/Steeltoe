// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
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

    public string OrgId { get; }
    public string SpaceId { get; }
    public string ApplicationId { get; }
    public string InstanceId { get; }

    private ApplicationInstanceCertificate(string orgId, string spaceId, string applicationId, string instanceId)
    {
        OrgId = orgId;
        SpaceId = spaceId;
        ApplicationId = applicationId;
        InstanceId = instanceId;
    }

    public static bool TryParse(string certificateSubject, [NotNullWhen(true)] out ApplicationInstanceCertificate? instanceCertificate)
    {
        instanceCertificate = null;
        certificateSubject = certificateSubject.Replace("\"", string.Empty, StringComparison.OrdinalIgnoreCase);

        Match instanceMatch = CloudFoundryInstanceCertificateSubjectRegex.Match(certificateSubject);

        if (!instanceMatch.Success)
        {
            instanceMatch = SteeltoeInstanceCertificateSubjectRegex.Match(certificateSubject);
        }

        if (instanceMatch.Success)
        {
            instanceCertificate = new ApplicationInstanceCertificate(instanceMatch.Groups["org"].Value, instanceMatch.Groups["space"].Value,
                instanceMatch.Groups["app"].Value, instanceMatch.Groups["instance"].Value);
        }

        return instanceMatch.Success;
    }
}

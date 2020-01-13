using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace Steeltoe.Security.Authentication.MtlsCore
{
    public class CloudFoundryInstanceCertificate
    {
        private static readonly string CloudFoundryInstanceCertSubjectRegex =
            @"^CN=(?<instance>[0-9a-f-]+),\sOU=organization:(?<org>[0-9a-f-]+)\s\+\sOU=space:(?<space>[0-9a-f-]+)\s\+\sOU=app:(?<app>[0-9a-f-]+)$";

        public static bool TryParse(X509Certificate2 certificate, out CloudFoundryInstanceCertificate cloudFoundryInstanceCertificate)
        {
            cloudFoundryInstanceCertificate = null;
            if (certificate == null)
            {
                return false;
            }

            var cfInstanceMatch = Regex.Match(certificate.SubjectName.Name, CloudFoundryInstanceCertSubjectRegex);
            if (cfInstanceMatch.Success)
            {
                cloudFoundryInstanceCertificate = new CloudFoundryInstanceCertificate();
                cloudFoundryInstanceCertificate.OrgId = cfInstanceMatch.Groups["org"].Value;
                cloudFoundryInstanceCertificate.SpaceId = cfInstanceMatch.Groups["space"].Value;
                cloudFoundryInstanceCertificate.AppId = cfInstanceMatch.Groups["app"].Value;
                cloudFoundryInstanceCertificate.InstanceId = cfInstanceMatch.Groups["instance"].Value;
                cloudFoundryInstanceCertificate.Certificate = certificate;
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
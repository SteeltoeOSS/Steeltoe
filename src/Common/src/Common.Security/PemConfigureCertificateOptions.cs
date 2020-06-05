// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Options;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace Steeltoe.Common.Security
{
    public class PemConfigureCertificateOptions : IConfigureNamedOptions<CertificateOptions>
    {
        private readonly IConfiguration _config;

        public PemConfigureCertificateOptions(IConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _config = config;
        }

        public void Configure(string name, CertificateOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.Name = name;

            var pemCert = _config["certificate"];
            var pemKey = _config["privateKey"];

            if (string.IsNullOrEmpty(pemCert) || string.IsNullOrEmpty(pemKey))
            {
                return;
            }

            var certChain = Regex.Matches(pemCert, "-+BEGIN CERTIFICATE-+.+?-+END CERTIFICATE-+", RegexOptions.Singleline)
               .Cast<Match>()
               .Select(x => new X509Certificate2(Encoding.Default.GetBytes(x.Value)))
               .ToList();

            options.Certificate = certChain.FirstOrDefault().CopyWithPrivateKey(ReadRsaKeyFromString(pemKey));

            options.IssuerChain = certChain
               .Skip(1)
               .Select(c => new X509Certificate2(c.GetRawCertData()))
               .ToList();
        }

        public void Configure(CertificateOptions options)
        {
            Configure(Microsoft.Extensions.Options.Options.DefaultName, options);
        }

        // source: https://stackoverflow.com/a/53439332/761468
        internal static RSA ReadRsaKeyFromString(string pemContents)
        {
            const string RsaPrivateKeyHeader = "-----BEGIN RSA PRIVATE KEY-----";
            const string RsaPrivateKeyFooter = "-----END RSA PRIVATE KEY-----";

            if (pemContents.StartsWith(RsaPrivateKeyHeader))
            {
                var endIdx = pemContents.IndexOf(
                    RsaPrivateKeyFooter,
                    RsaPrivateKeyHeader.Length,
                    StringComparison.Ordinal);

                var base64 = pemContents[RsaPrivateKeyHeader.Length..endIdx];

                var der = Convert.FromBase64String(base64);
                var rsa = RSA.Create();
                rsa.ImportRSAPrivateKey(der, out _);
                return rsa;
            }

            throw new InvalidOperationException();
        }
    }
}

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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

            options.Certificate = certChain.FirstOrDefault().CopyWithPrivateKey(ReadKeyFromString(pemKey));

            options.IssuerChain = certChain
               .Skip(1)
               .Select(c => new X509Certificate2(c.GetRawCertData()))
               .ToList();
        }

        public void Configure(CertificateOptions options)
        {
            Configure(Microsoft.Extensions.Options.Options.DefaultName, options);
        }

        private static RSA ReadKeyFromString(string pemContents)
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

            // "BEGIN PRIVATE KEY" (ImportPkcs8PrivateKey),
            // "BEGIN ENCRYPTED PRIVATE KEY" (ImportEncryptedPkcs8PrivateKey),
            // "BEGIN PUBLIC KEY" (ImportSubjectPublicKeyInfo),
            // "BEGIN RSA PUBLIC KEY" (ImportRSAPublicKey)
            // could any/all be handled here.
            throw new InvalidOperationException();
        }
    }
}

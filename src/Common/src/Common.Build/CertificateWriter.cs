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

using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Steeltoe.Common.Build
{
    public class CertificateWriter
    {
        public string PfxPassword { get; set; } = "St33ltoe5";

        public static readonly string AppBasePath = AppContext.BaseDirectory.Substring(0, AppContext.BaseDirectory.LastIndexOf(Path.DirectorySeparatorChar + "bin"));

        public string RootCAPfxPath { get; set; } = Path.Combine(Directory.GetParent(AppBasePath).ToString(), "GeneratedCertificates", "SteeltoeCA.pfx");

        public string IntermediatePfxPath { get; set; } = Path.Combine(Directory.GetParent(AppBasePath).ToString(), "GeneratedCertificates", "SteeltoeIntermediate.pfx");

        public bool Write(string orgId = null, string spaceId = null)
        {
            var appId = Guid.NewGuid();
            var instanceId = Guid.NewGuid();

            var subject = $"CN={instanceId},OU=organization:{orgId ?? Guid.NewGuid().ToString()},OU=space:{spaceId ?? Guid.NewGuid().ToString()},OU=app:{appId}";

            X509Certificate2 caCertificate;

            // Create Root CA PFX with private key (if not already there)
            if (!File.Exists(RootCAPfxPath))
            {
                caCertificate = CreateRoot("CN=SteeltoeGeneratedCA");
                File.WriteAllBytes(RootCAPfxPath, caCertificate.Export(X509ContentType.Pfx, PfxPassword));
            }
            else
            {
                caCertificate = new X509Certificate2(RootCAPfxPath, PfxPassword);
            }

            X509Certificate2 intermediateCertificate;

            // Create intermediate cert PFX with private key (if not already there)
            if (!File.Exists(IntermediatePfxPath))
            {
                intermediateCertificate = CreateIntermediate("CN=SteeltoeGeneratedIntermediate", caCertificate);
                File.WriteAllBytes(IntermediatePfxPath, intermediateCertificate.Export(X509ContentType.Pfx, PfxPassword));
            }
            else
            {
                intermediateCertificate = new X509Certificate2(IntermediatePfxPath, PfxPassword);
            }

            var clientCertificate = CreateClient(subject, intermediateCertificate, new SubjectAlternativeNameBuilder());

            // File.WriteAllBytes(Path.Combine("..", "GeneratedCertificates", "clientcert.pfx"), clientCertificate.Export(X509ContentType.Pfx))
            // Create Base 64 encoded CER (public key only)
            if (!Directory.Exists(Path.Combine(AppBasePath, "GeneratedCertificates")))
            {
                Directory.CreateDirectory(Path.Combine(AppBasePath, "GeneratedCertificates"));
            }

            var certContents =
                "-----BEGIN CERTIFICATE-----\r\n" +
                Convert.ToBase64String(clientCertificate.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks) +
                "\r\n-----END CERTIFICATE-----\r\n" +
                "-----BEGIN CERTIFICATE-----\r\n" +
                Convert.ToBase64String(intermediateCertificate.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks) +
                "\r\n-----END CERTIFICATE-----\r\n";

            File.WriteAllText(Path.Combine(AppBasePath, "GeneratedCertificates", "SteeltoeInstanceCert.pem"), certContents);

            File.WriteAllText(Path.Combine(AppBasePath, "GeneratedCertificates", "SteeltoeInstanceKey.pem"), "-----BEGIN RSA PRIVATE KEY-----\r\n" + Convert.ToBase64String(clientCertificate.GetRSAPrivateKey().ExportRSAPrivateKey(), Base64FormattingOptions.InsertLineBreaks) + "\r\n-----END RSA PRIVATE KEY-----");

            return true;
        }

        private static X509Certificate2 CreateRoot(string name)
        {
            using var key = RSA.Create();
            var request = new CertificateRequest(new X500DistinguishedName(name), key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));

            return request.CreateSelfSigned(DateTimeOffset.UtcNow, new DateTimeOffset(2039, 12, 31, 23, 59, 59, TimeSpan.Zero));
        }

        private static X509Certificate2 CreateIntermediate(string name, X509Certificate2 issuer)
        {
            using var key = RSA.Create();
            var request = new CertificateRequest(name, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));

            var serialNumber = new byte[8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(serialNumber);
            }

            return request.Create(issuer, DateTimeOffset.UtcNow, issuer.NotAfter, serialNumber).CopyWithPrivateKey(key);
        }

        private static X509Certificate2 CreateClient(string name, X509Certificate2 issuer, SubjectAlternativeNameBuilder altNames, DateTimeOffset? notAfter = null)
        {
            using var key = RSA.Create();
            var request = new CertificateRequest(name, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.2") }, false));

            if (altNames != null)
            {
                request.CertificateExtensions.Add(altNames.Build());
            }

            var serialNumber = new byte[8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(serialNumber);
            }

            var signedCert = request.Create(issuer, DateTimeOffset.UtcNow, notAfter ?? DateTimeOffset.UtcNow.AddDays(1), serialNumber);

            return signedCert.CopyWithPrivateKey(key);
        }
    }
}

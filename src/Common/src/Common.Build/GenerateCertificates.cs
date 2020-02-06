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

//using Org.BouncyCastle.Asn1;
//using Org.BouncyCastle.Asn1.Pkcs;
//using Org.BouncyCastle.Asn1.X509;
//using Org.BouncyCastle.Crypto;
//using Org.BouncyCastle.Crypto.Generators;
//using Org.BouncyCastle.Crypto.Operators;
//using Org.BouncyCastle.Crypto.Parameters;
//using Org.BouncyCastle.Crypto.Prng;
//using Org.BouncyCastle.Math;
//using Org.BouncyCastle.OpenSsl;
//using Org.BouncyCastle.Pkcs;
//using Org.BouncyCastle.Security;
//using Org.BouncyCastle.Utilities;
//using Org.BouncyCastle.X509;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Steeltoe.Common.Build
{
    public class GenerateCertificates : Microsoft.Build.Utilities.Task
    {
        public override bool Execute()
        {
            var instanceId = Guid.NewGuid();
            var appId = Guid.NewGuid();
            var spaceId = "fd7be6df-3c9b-4100-bcb8-823d29bad795"; // Guid.NewGuid()
            var orgId = "83b1519a-0ead-4fbc-998a-47f6ffe9a5d1"; // Guid.NewGuid()
            var subject = $"OU=organization:{orgId},OU=space:{spaceId},OU=app:{appId},CN={instanceId}";

            var caCertificate = CreateRoot("CN=SteeloeGeneratedCA");
            var clientCertificate = CreateClient(subject, caCertificate, new SubjectAlternativeNameBuilder());

            // Create PFX (PKCS #12) with private key
            File.WriteAllBytes(Path.Combine("..", "GeneratedCertifcates", "SteeltoeCA.pem"), caCertificate.Export(X509ContentType.Pkcs12, "P@55w0rd"));
            File.WriteAllBytes(Path.Combine("..", "GeneratedCertifcates", "SteeltoeCA.pem"), clientCertificate.Export(X509ContentType.Pkcs12, "P@55w0rd"));

            // Create Base 64 encoded CER (public key only)
            File.WriteAllText(Path.Combine("..", "..", "SteeltoeCA-pub.pem"), "-----BEGIN CERTIFICATE-----\r\n" + Convert.ToBase64String(caCertificate.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks) + "\r\n-----END CERTIFICATE-----");

            //var certEntry = new X509CertificateEntry(certificate);

            //var pkcs12Store = new Pkcs12StoreBuilder()
            //    .SetUseDerEncoding(true)
            //    .Build();

            //var keyEntry = new AsymmetricKeyEntry(subjectKeyPair.Private);
            //pkcs12Store.SetKeyEntry("ServerInstance", keyEntry, new X509CertificateEntry[] { certEntry });

            //byte[] bytes;
            //using (var stream = new MemoryStream())
            //{
            //    pkcs12Store.Save(stream, password?.ToArray(), new SecureRandom());
            //    bytes = Pkcs12Utilities.ConvertToDefiniteLength(stream.ToArray());

            //    // pfxCert = new System.Security.Cryptography.X509Certificates.X509Certificate2(bytes)
            //    var file = Path.GetTempFileName() + ".pfx";
            //    File.WriteAllBytes(file, bytes);
            //    Console.WriteLine($"PFX: {file}");
            //    Console.WriteLine($"PFX Base64: {Convert.ToBase64String(bytes)}");
            //}

            //var textWriter = new StringWriter();
            //var pemWriter = new PemWriter(textWriter);
            //pemWriter.WriteObject(certificate);
            //if (password == null)
            //{
            //    pemWriter.WriteObject(subjectKeyPair.Private);
            //}
            //else
            //{
            //    pemWriter.WriteObject(subjectKeyPair.Private, "DESEDE", password.ToArray(), new SecureRandom());
            //}

            //pemWriter.Writer.Flush();
            //Console.WriteLine(textWriter.ToString());

            return true;
        }

        // -----
        private static X509Certificate2 CreateRoot(string name)
        {
            // Creates a certificate roughly equivalent to 
            // makecert -r -n "{name}" -a sha256 -cy authority
            using (var key = ECDsa.Create())
            {
                var request = new CertificateRequest(new X500DistinguishedName(name), key, HashAlgorithmName.SHA256);

                request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));

                // makecert will add an authority key identifier extension, which .NET doesn't
                // have out of the box.
                //
                // It does not add a subject key identifier extension, so we won't, either.
                return request.CreateSelfSigned(DateTimeOffset.UtcNow, new DateTimeOffset(2039, 12, 31, 23, 59, 59, TimeSpan.Zero));
            }
        }

        private static X509Certificate2 CreateClient(string name, X509Certificate2 issuer, SubjectAlternativeNameBuilder altNames)
        {
            using (var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384))
            {
                var request = new CertificateRequest(name, ecdsa, HashAlgorithmName.SHA384);

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

                var signedCert = request.Create(
                    issuer,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow.AddDays(90),
                    serialNumber);

                return signedCert.CopyWithPrivateKey(ecdsa);
            }
        }
    }
}

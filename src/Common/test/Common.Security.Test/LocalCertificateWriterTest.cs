// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace Steeltoe.Common.Security.Test
{
    public class LocalCertificateWriterTest
    {
        [Fact]
        public void CertificatesIncludeParams()
        {
            var orgId = Guid.NewGuid();
            var spaceId = Guid.NewGuid();
            var certWriter = new LocalCertificateWriter();

            certWriter.Write(orgId, spaceId);
            var rootCertificate = new X509Certificate2(certWriter.RootCAPfxPath);
            var intermediateCert = new X509Certificate2(certWriter.IntermediatePfxPath);
            var clientCert =
                new X509Certificate2(File.ReadAllBytes(Path.Combine(LocalCertificateWriter.AppBasePath, "GeneratedCertificates", "SteeltoeInstanceCert.pem")))
                    .CopyWithPrivateKey(PemConfigureCertificateOptions.ReadRsaKeyFromString(File.ReadAllText(Path.Combine(LocalCertificateWriter.AppBasePath, "GeneratedCertificates", "SteeltoeInstanceKey.pem"))));

            Assert.NotNull(rootCertificate);
            Assert.NotNull(intermediateCert);
            Assert.NotNull(clientCert);
            Assert.Contains("OU=space:" + spaceId, clientCert.Subject);
            Assert.Contains("OU=organization:" + orgId, clientCert.Subject);
        }
    }
}

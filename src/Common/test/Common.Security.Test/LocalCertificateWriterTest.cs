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
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace Steeltoe.Common.Security.Test
{
    public class LocalCertificateWriterTest
    {
        [Fact]
        public void CertificatesIncludeParams()
        {
            // arrange
            var orgId = Guid.NewGuid().ToString();
            var spaceId = Guid.NewGuid().ToString();
            var certWriter = new LocalCertificateWriter();

            // act
            certWriter.Write(orgId, spaceId);
            var rootCertificate = new X509Certificate2(certWriter.RootCAPfxPath);
            var intermediateCert = new X509Certificate2(certWriter.IntermediatePfxPath);
            var clientCert =
                new X509Certificate2(File.ReadAllBytes(Path.Combine(LocalCertificateWriter.AppBasePath, "GeneratedCertificates", "SteeltoeInstanceCert.pem")))
                    .CopyWithPrivateKey(PemConfigureCertificateOptions.ReadRsaKeyFromString(File.ReadAllText(Path.Combine(LocalCertificateWriter.AppBasePath, "GeneratedCertificates", "SteeltoeInstanceKey.pem"))));

            // assert
            Assert.NotNull(rootCertificate);
            Assert.NotNull(intermediateCert);
            Assert.NotNull(clientCert);
            Assert.Contains("OU=space:" + spaceId, clientCert.Subject);
            Assert.Contains("OU=organization:" + orgId, clientCert.Subject);
        }
    }
}

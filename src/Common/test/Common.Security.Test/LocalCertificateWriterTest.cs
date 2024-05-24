// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Xunit;

namespace Steeltoe.Common.Security.Test;

public sealed class LocalCertificateWriterTest
{
    [Fact]
    public void CertificatesIncludeParams()
    {
        var orgId = Guid.NewGuid();
        var spaceId = Guid.NewGuid();
        var certWriter = new LocalCertificateWriter();
        var rsa = RSA.Create();

        certWriter.Write(orgId, spaceId);
        var rootCertificate = new X509Certificate2(certWriter.RootCertificateAuthorityPfxPath);
        var intermediateCert = new X509Certificate2(certWriter.IntermediatePfxPath);

        rsa.ImportFromPem(File.ReadAllText(Path.Combine(LocalCertificateWriter.ApplicationBasePath, "GeneratedCertificates", "SteeltoeInstanceKey.pem")));

        X509Certificate2 clientCert =
            new X509Certificate2(File.ReadAllBytes(Path.Combine(LocalCertificateWriter.ApplicationBasePath, "GeneratedCertificates", "SteeltoeInstanceCert.pem")))
                .CopyWithPrivateKey(rsa);

        rootCertificate.Should().NotBeNull();
        intermediateCert.Should().NotBeNull();
        clientCert.Should().NotBeNull();
        clientCert.Subject.Should().Contain($"OU=space:{spaceId}");
        clientCert.Subject.Should().Contain($"OU=organization:{orgId}");
    }
}

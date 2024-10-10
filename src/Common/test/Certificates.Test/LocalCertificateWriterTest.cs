// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Steeltoe.Common.Certificates.Test;

public sealed class LocalCertificateWriterTest
{
    [Fact]
    public void CertificatesIncludeParams()
    {
        var orgId = Guid.NewGuid();
        var spaceId = Guid.NewGuid();
        var certificateWriter = new LocalCertificateWriter();
        using var rsa = RSA.Create();

        certificateWriter.Write(orgId, spaceId);
#pragma warning disable SYSLIB0057 // Type or member is obsolete
        var rootCertificate = new X509Certificate2(LocalCertificateWriter.RootCaPfxPath);
        var intermediateCertificate = new X509Certificate2(LocalCertificateWriter.IntermediatePfxPath);
#pragma warning restore SYSLIB0057 // Type or member is obsolete

        rsa.ImportFromPem(File.ReadAllText(Path.Combine(LocalCertificateWriter.AppBasePath, LocalCertificateWriter.CertificateDirectoryName,
            "SteeltoeAppInstanceKey.pem")));

        X509Certificate2 certificate =
#pragma warning disable SYSLIB0057 // Type or member is obsolete
            new X509Certificate2(File.ReadAllBytes(Path.Combine(LocalCertificateWriter.AppBasePath, LocalCertificateWriter.CertificateDirectoryName,
                "SteeltoeAppInstanceCert.pem"))).CopyWithPrivateKey(rsa);
#pragma warning restore SYSLIB0057 // Type or member is obsolete

        rootCertificate.Should().NotBeNull();
        intermediateCertificate.Should().NotBeNull();
        certificate.Should().NotBeNull();
        certificate.Subject.Should().Contain($"OU=space:{spaceId}");
        certificate.Subject.Should().Contain($"OU=organization:{orgId}");
    }
}

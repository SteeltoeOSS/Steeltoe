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
        var certificateWriter = new LocalCertificateWriter(TimeProvider.System);
        using var rsa = RSA.Create();

        certificateWriter.Write(orgId, spaceId);

        var rootCertificate = X509Certificate2.CreateFromPem(File.ReadAllText(LocalCertificateWriter.RootCaCrtPath));
        var intermediateCertificate = X509Certificate2.CreateFromPem(File.ReadAllText(LocalCertificateWriter.IntermediateCrtPath));

        rsa.ImportFromPem(File.ReadAllText(Path.Combine(LocalCertificateWriter.AppBasePath, LocalCertificateWriter.CertificateDirectoryName,
            "SteeltoeAppInstanceKey.pem")));

        string instanceCertificatePath = Path.Combine(LocalCertificateWriter.AppBasePath, LocalCertificateWriter.CertificateDirectoryName,
            "SteeltoeAppInstanceCert.pem");

        X509Certificate2 certificate = X509Certificate2.CreateFromPem(File.ReadAllText(instanceCertificatePath)).CopyWithPrivateKey(rsa);
        certificate.Subject.Should().Contain($"OU=space:{spaceId}");
        certificate.Subject.Should().Contain($"OU=organization:{orgId}");

        rootCertificate.Subject.Should().Contain("SteeltoeGeneratedCA");
        intermediateCertificate.Subject.Should().Contain("SteeltoeGeneratedIntermediate");
    }
}

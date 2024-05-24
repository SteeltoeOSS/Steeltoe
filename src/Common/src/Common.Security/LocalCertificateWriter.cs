// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Steeltoe.Common.Security;

internal sealed class LocalCertificateWriter
{
    public static readonly string ApplicationBasePath =
        AppContext.BaseDirectory[..AppContext.BaseDirectory.LastIndexOf($"{Path.DirectorySeparatorChar}bin", StringComparison.Ordinal)];

    internal string CertificateFilenamePrefix { get; set; } = "SteeltoeInstance";

    public string RootCertificateAuthorityPfxPath { get; set; } =
        Path.Combine(Directory.GetParent(ApplicationBasePath).ToString(), "GeneratedCertificates", "SteeltoeCA.pfx");

    public string IntermediatePfxPath { get; set; } =
        Path.Combine(Directory.GetParent(ApplicationBasePath).ToString(), "GeneratedCertificates", "SteeltoeIntermediate.pfx");

    public void Write(Guid orgId, Guid spaceId)
    {
        var appId = Guid.NewGuid();
        var instanceId = Guid.NewGuid();

        // Certificates provided by Diego will have a subject that doesn't comply with standards.
        // System.Security.Cryptography.X509Certificates.CertificateRequest would re-order these components to comply if we tried to match.
        // Non-compliant subject looks like this: "CN={instanceId}, OU=organization:{organizationId} + OU=space:{spaceId} + OU=app:{appId}"
        string subject = $"CN={instanceId}, OU=app:{appId} + OU=space:{spaceId} + OU=organization:{orgId}";

        X509Certificate2 rootAuthorityCertificate;

        // Create a directory a level above the running project to contain the root and intermediate certificates
        if (!Directory.Exists(Path.Combine(Directory.GetParent(ApplicationBasePath).ToString(), "GeneratedCertificates")))
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetParent(ApplicationBasePath).ToString(), "GeneratedCertificates"));
        }

        // Create the root certificate if it doesn't already exist (can be shared by multiple applications)
        if (!File.Exists(RootCertificateAuthorityPfxPath))
        {
            rootAuthorityCertificate = CreateRootCertificate("CN=SteeltoeGeneratedCA");
            File.WriteAllBytes(RootCertificateAuthorityPfxPath, rootAuthorityCertificate.Export(X509ContentType.Pfx));
        }
        else
        {
            rootAuthorityCertificate = new X509Certificate2(RootCertificateAuthorityPfxPath);
        }

        // Create the intermediate certificate if it doesn't already exist (can be shared by multiple applications)
        X509Certificate2 intermediateCertificate;

        if (!File.Exists(IntermediatePfxPath))
        {
            intermediateCertificate = CreateIntermediateCertificate("CN=SteeltoeGeneratedIntermediate", rootAuthorityCertificate);
            File.WriteAllBytes(IntermediatePfxPath, intermediateCertificate.Export(X509ContentType.Pfx));
        }
        else
        {
            intermediateCertificate = new X509Certificate2(IntermediatePfxPath);
        }

        X509Certificate2 clientCertificate = CreateClientCertificate(subject, intermediateCertificate, new SubjectAlternativeNameBuilder());

        // Create a folder inside the project to store generated certificate files
        if (!Directory.Exists(Path.Combine(ApplicationBasePath, "GeneratedCertificates")))
        {
            Directory.CreateDirectory(Path.Combine(ApplicationBasePath, "GeneratedCertificates"));
        }

        string chainedCertificateContents;
#if NET8_0_OR_GREATER
        chainedCertificateContents = clientCertificate.ExportCertificatePem() + "\r\n" + intermediateCertificate.ExportCertificatePem();
#else
        chainedCertificateContents = "-----BEGIN CERTIFICATE-----\r\n" + Convert.ToBase64String(clientCertificate.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks) + "\r\n-----END CERTIFICATE-----\r\n" + "-----BEGIN CERTIFICATE-----\r\n" + Convert.ToBase64String(intermediateCertificate.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks) + "\r\n-----END CERTIFICATE-----\r\n";
#endif

        string keyContents = "-----BEGIN RSA PRIVATE KEY-----\r\n" +
            Convert.ToBase64String(clientCertificate.GetRSAPrivateKey()!.ExportRSAPrivateKey(), Base64FormattingOptions.InsertLineBreaks) +
            "\r\n-----END RSA PRIVATE KEY-----";

        File.WriteAllText(Path.Combine(ApplicationBasePath, "GeneratedCertificates", CertificateFilenamePrefix + "Cert.pem"), chainedCertificateContents);
        File.WriteAllText(Path.Combine(ApplicationBasePath, "GeneratedCertificates", CertificateFilenamePrefix + "Key.pem"), keyContents);
    }

    private static X509Certificate2 CreateRootCertificate(string distinguishedName)
    {
        using var privateKey = RSA.Create();

        var certificateRequest =
            new CertificateRequest(new X500DistinguishedName(distinguishedName), privateKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        certificateRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));

        return certificateRequest.CreateSelfSigned(DateTimeOffset.UtcNow, new DateTimeOffset(2039, 12, 31, 23, 59, 59, TimeSpan.Zero));
    }

    private static X509Certificate2 CreateIntermediateCertificate(string subjectName, X509Certificate2 issuerCertificate)
    {
        using var privateKey = RSA.Create();
        var certificateRequest = new CertificateRequest(subjectName, privateKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        certificateRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));

        byte[] serialNumber = new byte[8];
        using var randomNumberGenerator = RandomNumberGenerator.Create();
        randomNumberGenerator.GetBytes(serialNumber);

        return certificateRequest.Create(issuerCertificate, DateTimeOffset.UtcNow, issuerCertificate.NotAfter, serialNumber).CopyWithPrivateKey(privateKey);
    }

    private static X509Certificate2 CreateClientCertificate(string subjectName, X509Certificate2 issuerCertificate,
        SubjectAlternativeNameBuilder alternativeNames, DateTimeOffset? notAfter = null)
    {
        using var privateKey = RSA.Create();
        var request = new CertificateRequest(subjectName, privateKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));

        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([
            new Oid("1.3.6.1.5.5.7.3.1"), // serverAuth
            new Oid("1.3.6.1.5.5.7.3.2") // clientAuth
        ], false));

        if (alternativeNames != null)
        {
            request.CertificateExtensions.Add(alternativeNames.Build());
        }

        byte[] serialNumber = new byte[8];

        using (var randomNumberGenerator = RandomNumberGenerator.Create())
        {
            randomNumberGenerator.GetBytes(serialNumber);
        }

        X509Certificate2 signedCertificate =
            request.Create(issuerCertificate, DateTimeOffset.UtcNow, notAfter ?? DateTimeOffset.UtcNow.AddDays(1), serialNumber);

        return signedCertificate.CopyWithPrivateKey(privateKey);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Steeltoe.Common.Security;

public class LocalCertificateWriter
{
    internal string CertificateFilenamePrefix { get; set; } = "SteeltoeInstance";

    public static readonly string AppBasePath = AppContext.BaseDirectory.Substring(0, AppContext.BaseDirectory.LastIndexOf(
        $"{Path.DirectorySeparatorChar}bin"));

    public string RootCaPfxPath { get; set; } = Path.Combine(Directory.GetParent(AppBasePath).ToString(), "GeneratedCertificates", "SteeltoeCA.pfx");

    public string IntermediatePfxPath { get; set; } = Path.Combine(Directory.GetParent(AppBasePath).ToString(), "GeneratedCertificates", "SteeltoeIntermediate.pfx");

    public bool Write(Guid orgId, Guid spaceId)
    {
        var appId = Guid.NewGuid();
        var instanceId = Guid.NewGuid();

        // Certificates provided by Diego will have a subject that doesn't comply with standards, but CertificateRequest would re-order these components anyway
        // Diego subjects will look like this: "CN=<instanceId>, OU=organization:<organizationId> + OU=space:<spaceId> + OU=app:<appId>"
        var subject = $"CN={instanceId}, OU=app:{appId} + OU=space:{spaceId} + OU=organization:{orgId}";

        X509Certificate2 caCertificate;

        // Create Root CA and intermediate cert PFX with private key (if not already there)
        if (!Directory.Exists(Path.Combine(Directory.GetParent(AppBasePath).ToString(), "GeneratedCertificates")))
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetParent(AppBasePath).ToString(), "GeneratedCertificates"));
        }

        if (!File.Exists(RootCaPfxPath))
        {
            caCertificate = CreateRoot("CN=SteeltoeGeneratedCA");
            File.WriteAllBytes(RootCaPfxPath, caCertificate.Export(X509ContentType.Pfx));
        }
        else
        {
            caCertificate = new X509Certificate2(RootCaPfxPath);
        }

        X509Certificate2 intermediateCertificate;

        // Create intermediate cert PFX with private key (if not already there)
        if (!File.Exists(IntermediatePfxPath))
        {
            intermediateCertificate = CreateIntermediate("CN=SteeltoeGeneratedIntermediate", caCertificate);
            File.WriteAllBytes(IntermediatePfxPath, intermediateCertificate.Export(X509ContentType.Pfx));
        }
        else
        {
            intermediateCertificate = new X509Certificate2(IntermediatePfxPath);
        }

        var clientCertificate = CreateClient(subject, intermediateCertificate, new SubjectAlternativeNameBuilder());

        // Create a folder inside the project to store generated certificate files
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

        var keyContents =
            "-----BEGIN RSA PRIVATE KEY-----\r\n" +
            Convert.ToBase64String(clientCertificate.GetRSAPrivateKey().ExportRSAPrivateKey(), Base64FormattingOptions.InsertLineBreaks) +
            "\r\n-----END RSA PRIVATE KEY-----";

        File.WriteAllText(Path.Combine(AppBasePath, "GeneratedCertificates", CertificateFilenamePrefix + "Cert.pem"), certContents);
        File.WriteAllText(Path.Combine(AppBasePath, "GeneratedCertificates", CertificateFilenamePrefix + "Key.pem"), keyContents);

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
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(serialNumber);

        return request.Create(issuer, DateTimeOffset.UtcNow, issuer.NotAfter, serialNumber).CopyWithPrivateKey(key);
    }

    private static X509Certificate2 CreateClient(string name, X509Certificate2 issuer, SubjectAlternativeNameBuilder altNames, DateTimeOffset? notAfter = null)
    {
        using var key = RSA.Create();
        var request = new CertificateRequest(name, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection
            {
                new ("1.3.6.1.5.5.7.3.1"), // serverAuth
                new ("1.3.6.1.5.5.7.3.2") // clientAuth
            }, false));

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

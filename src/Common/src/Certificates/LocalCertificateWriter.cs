// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Steeltoe.Common.Certificates;

internal sealed class LocalCertificateWriter
{
    internal const string CertificateDirectoryName = "GeneratedCertificates";
    internal const string CertificateFilenamePrefix = "SteeltoeAppInstance";
    internal static readonly string AppBasePath = GetAppBasePath();
    private static readonly string ParentPath = Directory.GetParent(AppBasePath)?.ToString() ?? string.Empty;

    // PKCS#12 files contain the private keys used to sign new instance certificates.
    private static readonly string SigningAuthoritiesPath = Path.Combine(ParentPath, CertificateDirectoryName, "signing");
    private static readonly string RootCaSigningPfxPath = Path.Combine(SigningAuthoritiesPath, "SteeltoeCA.pfx");
    private static readonly string IntermediateSigningPfxPath = Path.Combine(SigningAuthoritiesPath, "SteeltoeIntermediate.pfx");

    // CF_SYSTEM_CERT_PATH contains one PEM-encoded CA certificate per .crt file.
    // This directory mirrors that layout so the local simulation matches real CF behavior.
    internal static readonly string SystemCertPath = Path.Combine(ParentPath, CertificateDirectoryName, "trust");
    internal static readonly string RootCaCrtPath = Path.Combine(SystemCertPath, "SteeltoeCA.crt");
    internal static readonly string IntermediateCrtPath = Path.Combine(SystemCertPath, "SteeltoeIntermediate.crt");

    private readonly TimeProvider _timeProvider;

    public LocalCertificateWriter(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        _timeProvider = timeProvider;
    }

    private static string GetAppBasePath()
    {
        if (AppContext.BaseDirectory.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            // Traverse up to the project directory, if running from IDE. Strips off a sub-path like: \bin\Debug\net10.0\win-x64\
            return AppContext.BaseDirectory[..AppContext.BaseDirectory.LastIndexOf($"{Path.DirectorySeparatorChar}bin", StringComparison.Ordinal)];
        }

        return AppContext.BaseDirectory[..^1];
    }

    public void Write(Guid orgId, Guid spaceId)
    {
        var appId = Guid.NewGuid();
        var instanceId = Guid.NewGuid();

        // Certificates provided by Cloud Foundry will have a subject that doesn't comply with standards.
        // System.Security.Cryptography.X509Certificates.CertificateRequest would re-order these components to comply if we tried to match.
        // Non-compliant subject looks like this: "CN={instanceId}, OU=organization:{organizationId} + OU=space:{spaceId} + OU=app:{appId}"
        string subject = $"CN={instanceId}, OU=app:{appId} + OU=space:{spaceId} + OU=organization:{orgId}";

        X509Certificate2 caCertificate;

        // CA materials go one level up so they can be shared by multiple applications in the same solution.
        Directory.CreateDirectory(SigningAuthoritiesPath);
        Directory.CreateDirectory(SystemCertPath);

        // Create the root certificate if it doesn't already exist (can be shared by multiple applications)
        if (!File.Exists(RootCaSigningPfxPath))
        {
            caCertificate = CreateRootCertificate("CN=SteeltoeGeneratedCA");
            File.WriteAllBytes(RootCaSigningPfxPath, caCertificate.Export(X509ContentType.Pfx));
        }
        else
        {
            caCertificate = X509CertificateLoader.LoadPkcs12FromFile(RootCaSigningPfxPath, null);
        }

        File.WriteAllText(RootCaCrtPath, caCertificate.ExportCertificatePem());

        // Create the intermediate certificate if it doesn't already exist (can be shared by multiple applications)
        X509Certificate2 intermediateCertificate;

        if (!File.Exists(IntermediateSigningPfxPath))
        {
            intermediateCertificate = CreateIntermediateCertificate("CN=SteeltoeGeneratedIntermediate", caCertificate);
            File.WriteAllBytes(IntermediateSigningPfxPath, intermediateCertificate.Export(X509ContentType.Pfx));
        }
        else
        {
            intermediateCertificate = X509CertificateLoader.LoadPkcs12FromFile(IntermediateSigningPfxPath, null);
        }

        File.WriteAllText(IntermediateCrtPath, intermediateCertificate.ExportCertificatePem());

        var subjectAlternativeNameBuilder = new SubjectAlternativeNameBuilder();

        X509Certificate2 clientCertificate = CreateClientCertificate(subject, intermediateCertificate, subjectAlternativeNameBuilder);

        // Instance certificates are per-application and are written under the application directory.
        Directory.CreateDirectory(Path.Combine(AppBasePath, CertificateDirectoryName));

        string chainedCertificateContents = clientCertificate.ExportCertificatePem() + Environment.NewLine + intermediateCertificate.ExportCertificatePem();
        string keyContents = clientCertificate.GetRSAPrivateKey()!.ExportRSAPrivateKeyPem();

        File.WriteAllText(Path.Combine(AppBasePath, CertificateDirectoryName, $"{CertificateFilenamePrefix}Cert.pem"), chainedCertificateContents);
        File.WriteAllText(Path.Combine(AppBasePath, CertificateDirectoryName, $"{CertificateFilenamePrefix}Key.pem"), keyContents);
    }

    private X509Certificate2 CreateRootCertificate(string distinguishedName)
    {
        using var privateKey = RSA.Create();

        var certificateRequest =
            new CertificateRequest(new X500DistinguishedName(distinguishedName), privateKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        certificateRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));

        return certificateRequest.CreateSelfSigned(_timeProvider.GetUtcNow(), new DateTimeOffset(2039, 12, 31, 23, 59, 59, TimeSpan.Zero));
    }

    private X509Certificate2 CreateIntermediateCertificate(string subjectName, X509Certificate2 issuerCertificate)
    {
        using var privateKey = RSA.Create();
        var certificateRequest = new CertificateRequest(subjectName, privateKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        certificateRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));

        byte[] serialNumber = new byte[8];
        using var randomNumberGenerator = RandomNumberGenerator.Create();
        randomNumberGenerator.GetBytes(serialNumber);

        return certificateRequest.Create(issuerCertificate, _timeProvider.GetUtcNow(), issuerCertificate.NotAfter, serialNumber).CopyWithPrivateKey(privateKey);
    }

    private X509Certificate2 CreateClientCertificate(string subjectName, X509Certificate2 issuerCertificate, SubjectAlternativeNameBuilder alternativeNames,
        DateTimeOffset? notAfter = null)
    {
        using var privateKey = RSA.Create();
        var request = new CertificateRequest(subjectName, privateKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));

        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([
            new Oid("1.3.6.1.5.5.7.3.1"), // serverAuth
            new Oid("1.3.6.1.5.5.7.3.2") // clientAuth
        ], false));

        request.CertificateExtensions.Add(alternativeNames.Build());

        byte[] serialNumber = new byte[8];

        using (var randomNumberGenerator = RandomNumberGenerator.Create())
        {
            randomNumberGenerator.GetBytes(serialNumber);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        X509Certificate2 signedCertificate = request.Create(issuerCertificate, utcNow, notAfter ?? utcNow.AddDays(1), serialNumber);
        return signedCertificate.CopyWithPrivateKey(privateKey);
    }
}

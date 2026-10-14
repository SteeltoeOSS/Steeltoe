// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Steeltoe.Common.Certificates;

internal sealed partial class ConfigureCertificateOptions : IConfigureNamedOptions<CertificateOptions>
{
    private const int RegexMatchTimeoutInMilliseconds = 1_000;

    private readonly IConfiguration _configuration;

    public ConfigureCertificateOptions(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _configuration = configuration;
    }

    [GeneratedRegex("-+BEGIN CERTIFICATE-+.+?-+END CERTIFICATE-+", RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        RegexMatchTimeoutInMilliseconds)]
    private static partial Regex PemCertificateRegex();

    public void Configure(CertificateOptions options)
    {
        Configure(Options.DefaultName, options);
    }

    public void Configure(string? name, CertificateOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        string? certificateFilePath = _configuration.GetValue<string>(GetConfigurationKey(name, "CertificateFilePath"));

        if (options.Certificate != null || certificateFilePath == null || !File.Exists(certificateFilePath))
        {
            return;
        }

        string? privateKeyFilePath = _configuration.GetValue<string>(GetConfigurationKey(name, "PrivateKeyFilePath"));
        bool isPkcs12 = false;

        if (!string.IsNullOrEmpty(privateKeyFilePath) && File.Exists(privateKeyFilePath))
        {
            options.Certificate = LoadCertificateWithKey(certificateFilePath, privateKeyFilePath);
        }
        else
        {
            // isPkcs12 only applies when there is no separate private key file.
            isPkcs12 = certificateFilePath.EndsWith(".p12", StringComparison.OrdinalIgnoreCase) ||
                certificateFilePath.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase);

            options.Certificate = LoadCertificate(certificateFilePath, isPkcs12);
        }

        X509Certificate2Collection issuerChain = isPkcs12
            ? LoadIssuerChainFromPkcs12(certificateFilePath, options.Certificate.Thumbprint)
            : LoadIssuerChainFromPem(certificateFilePath);

        foreach (X509Certificate2 certificate in issuerChain)
        {
            options.IssuerChain.Add(certificate);
        }
    }

    private static X509Certificate2 LoadCertificate(string certificateFilePath, bool isPkcs12)
    {
        // LoadCertificateFromFile handles PEM and DER but not PKCS#12; .pfx/.p12 need a separate loader.
        return isPkcs12
            ? X509CertificateLoader.LoadPkcs12FromFile(certificateFilePath, null)
            : X509CertificateLoader.LoadCertificateFromFile(certificateFilePath);
    }

    private static X509Certificate2 LoadCertificateWithKey(string certificateFilePath, string privateKeyFilePath)
    {
        if (Platform.IsWindows)
        {
            // CreateFromPemFile loads the private key with EphemeralKeySet. Windows Schannel rejects
            // ephemeral keys for TLS handshakes, so the certificate must be re-imported with UserKeySet.
            // See https://learn.microsoft.com/dotnet/core/extensions/sslstream-troubleshooting
            using var certificate = X509Certificate2.CreateFromPemFile(certificateFilePath, privateKeyFilePath);
            byte[] pkcs12Bytes = certificate.Export(X509ContentType.Pkcs12);
            return X509CertificateLoader.LoadPkcs12(pkcs12Bytes, null, X509KeyStorageFlags.UserKeySet);
        }

        return X509Certificate2.CreateFromPemFile(certificateFilePath, privateKeyFilePath);
    }

    private static X509Certificate2Collection LoadIssuerChainFromPkcs12(string certificateFilePath, string skipThumbprint)
    {
        // PKCS#12 does not guarantee certificate ordering, so the leaf cannot be identified by position.
        // The leaf thumbprint is used to exclude it; all other certificates are chain members.
        var issuerChain = new X509Certificate2Collection();
        X509Certificate2Collection fullCertificateChain = X509CertificateLoader.LoadPkcs12CollectionFromFile(certificateFilePath, null);

        foreach (X509Certificate2 certificate in fullCertificateChain)
        {
            if (certificate.Thumbprint != skipThumbprint)
            {
                issuerChain.Add(certificate);
            }
            else
            {
                certificate.Dispose();
            }
        }

        return issuerChain;
    }

    private static X509Certificate2Collection LoadIssuerChainFromPem(string certificateFilePath)
    {
        // PEM files encode certificates in document order with the leaf first by convention.
        // Skip(1) relies on that ordering; the remaining matches are chain certificates.
        var issuerChain = new X509Certificate2Collection();

        foreach (Match match in PemCertificateRegex().Matches(File.ReadAllText(certificateFilePath)).Skip(1))
        {
            issuerChain.Add(X509Certificate2.CreateFromPem(match.Value));
        }

        return issuerChain;
    }

    private static string GetConfigurationKey(string? optionName, string propertyName)
    {
        return string.IsNullOrEmpty(optionName)
            ? string.Join(ConfigurationPath.KeyDelimiter, CertificateOptions.ConfigurationKeyPrefix, propertyName)
            : string.Join(ConfigurationPath.KeyDelimiter, CertificateOptions.ConfigurationKeyPrefix, optionName, propertyName);
    }
}

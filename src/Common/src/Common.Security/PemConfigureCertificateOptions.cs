// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Options;

namespace Steeltoe.Common.Security;

public sealed class PemConfigureCertificateOptions : IConfigureNamedOptions<CertificateOptions>
{
    private readonly IConfiguration _configuration;

    public PemConfigureCertificateOptions(IConfiguration configuration)
    {
        ArgumentGuard.NotNull(configuration);

        _configuration = configuration;
    }

    public void Configure(CertificateOptions options)
    {
        Configure(options.Name, options);
    }

    public void Configure(string name, CertificateOptions options)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        ArgumentGuard.NotNull(options);

        string pemCert = _configuration[$"{CertificateOptions.ConfigurationPrefix}:{name}:certificate"];
        string pemKey = _configuration[$"{CertificateOptions.ConfigurationPrefix}:{name}:privateKey"];

        if (string.IsNullOrEmpty(pemCert) || string.IsNullOrEmpty(pemKey))
        {
            return;
        }

        List<X509Certificate2> certChain = Regex
            .Matches(pemCert, "-+BEGIN CERTIFICATE-+.+?-+END CERTIFICATE-+", RegexOptions.Singleline, TimeSpan.FromSeconds(1))
            .Select(x => new X509Certificate2(Encoding.Default.GetBytes(x.Value))).ToList();

        options.Certificate = certChain.FirstOrDefault().CopyWithPrivateKey(ReadRsaKeyFromString(pemKey));

        options.IssuerChain = certChain.Skip(1).Select(c => new X509Certificate2(c.GetRawCertData())).ToList();
    }

    // source: https://stackoverflow.com/a/53439332/761468
    internal static RSA ReadRsaKeyFromString(string pemContents)
    {
        const string rsaPrivateKeyHeader = "-----BEGIN RSA PRIVATE KEY-----";
        const string rsaPrivateKeyFooter = "-----END RSA PRIVATE KEY-----";

        if (pemContents.StartsWith(rsaPrivateKeyHeader, StringComparison.Ordinal))
        {
            int endIdx = pemContents.IndexOf(rsaPrivateKeyFooter, rsaPrivateKeyHeader.Length, StringComparison.Ordinal);

            string base64 = pemContents[rsaPrivateKeyHeader.Length..endIdx];

            byte[] der = Convert.FromBase64String(base64);
            var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(der, out _);
            return rsa;
        }

        throw new InvalidOperationException();
    }
}

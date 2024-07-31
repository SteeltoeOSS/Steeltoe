// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Configuration;

namespace Steeltoe.Common.Certificates;

internal sealed class ConfigureCertificateOptions : IConfigureNamedOptions<CertificateOptions>
{
    private static readonly Regex CertificateRegex = new("-+BEGIN CERTIFICATE-+.+?-+END CERTIFICATE-+", RegexOptions.Compiled | RegexOptions.Singleline,
        TimeSpan.FromSeconds(1));

    private readonly IConfiguration _configuration;

    public ConfigureCertificateOptions(IConfiguration configuration)
    {
        ArgumentGuard.NotNull(configuration);

        _configuration = configuration;
    }

    public void Configure(CertificateOptions options)
    {
        Configure(Options.DefaultName, options);
    }

    public void Configure(string? name, CertificateOptions options)
    {
        ArgumentGuard.NotNull(options);

        string? certificateFilePath = _configuration.GetValue<string>(GetConfigurationKey(name, "CertificateFilePath"));

        if (options.Certificate != null || certificateFilePath == null || !File.Exists(certificateFilePath))
        {
            return;
        }

        string? privateKeyFilePath = _configuration.GetValue<string>(GetConfigurationKey(name, "PrivateKeyFilePath"));

        options.Certificate = privateKeyFilePath != null && File.Exists(privateKeyFilePath)
            ? X509Certificate2.CreateFromPemFile(certificateFilePath, privateKeyFilePath)
            : new X509Certificate2(certificateFilePath);

        List<X509Certificate2> certChain = CertificateRegex.Matches(File.ReadAllText(certificateFilePath))
            .Select(x => new X509Certificate2(Encoding.ASCII.GetBytes(x.Value))).ToList();

        foreach (X509Certificate2 issuer in certChain.Skip(1))
        {
            options.IssuerChain.Add(issuer);
        }
    }

    private static string GetConfigurationKey(string? optionName, string propertyName)
    {
        return string.IsNullOrEmpty(optionName)
            ? string.Join(ConfigurationPath.KeyDelimiter, CertificateOptions.ConfigurationKeyPrefix, propertyName)
            : string.Join(ConfigurationPath.KeyDelimiter, CertificateOptions.ConfigurationKeyPrefix, optionName, propertyName);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Configuration;

namespace Steeltoe.Common.Security;

public sealed class ConfigureCertificateOptions : IConfigureNamedOptions<CertificateOptions>
{
    private static readonly Regex CertificateRegex = new("-+BEGIN CERTIFICATE-+.+?-+END CERTIFICATE-+", RegexOptions.Compiled | RegexOptions.Singleline,
        TimeSpan.FromSeconds(1));

    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigureCertificateOptions> _logger;

    public ConfigureCertificateOptions(IConfiguration configuration, ILogger<ConfigureCertificateOptions> logger)
    {
        ArgumentGuard.NotNull(configuration);

        _configuration = configuration;
        _logger = logger;
    }

    public void Configure(CertificateOptions options)
    {
        Configure(Microsoft.Extensions.Options.Options.DefaultName, options);
    }

    public void Configure(string? name, CertificateOptions options)
    {
        ArgumentGuard.NotNull(options);

        if (options.Certificate != null)
        {
            return;
        }

        string? certificateFilePath = _configuration.GetValue<string>(GetConfigurationKey(name, "CertificateFilePath"));
        string? certificateContents = SafeReadAllText(certificateFilePath);

        if (string.IsNullOrEmpty(certificateFilePath) || string.IsNullOrEmpty(certificateContents))
        {
            return;
        }

        string? privateKeyFilePath = _configuration.GetValue<string>(GetConfigurationKey(name, "PrivateKeyFilePath"));

        if (!string.IsNullOrEmpty(privateKeyFilePath))
        {
            List<X509Certificate2> certChain = CertificateRegex.Matches(certificateContents).Select(x => new X509Certificate2(Encoding.ASCII.GetBytes(x.Value)))
                .ToList();

            string? keyData = SafeReadAllText(privateKeyFilePath);

            using var key = RSA.Create();
            key.ImportFromPem(keyData!.ToCharArray());

            options.Certificate = certChain[0].CopyWithPrivateKey(key);

            foreach (X509Certificate2 issuer in certChain.Skip(1))
            {
                options.IssuerChain.Add(issuer);
            }
        }
        else
        {
            options.Certificate = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? new X509Certificate2(certificateFilePath)
                : new X509Certificate2(certificateFilePath, string.Empty, X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);
        }
    }

    private static string GetConfigurationKey(string? optionName, string propertyName)
    {
        return string.IsNullOrEmpty(optionName)
            ? string.Join(ConfigurationPath.KeyDelimiter, CertificateOptions.ConfigurationKeyPrefix, propertyName)
            : string.Join(ConfigurationPath.KeyDelimiter, CertificateOptions.ConfigurationKeyPrefix, optionName, propertyName);
    }

    private string? SafeReadAllText(string? path)
    {
        if (path != null)
        {
            try
            {
                return File.ReadAllText(path);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to read file while configuring certificates.");
            }
        }

        return null;
    }
}

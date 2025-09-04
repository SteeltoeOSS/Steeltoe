// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Common;

/// <summary>
/// Provides security-focused validation for application configuration.
/// </summary>
internal static class SecurityConfigurationValidator
{
    /// <summary>
    /// Validates security-related configuration settings and logs warnings for insecure configurations.
    /// </summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <param name="logger">Logger for reporting security warnings.</param>
    public static void ValidateSecurityConfiguration(IConfiguration configuration, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);

        ValidateCertificateSettings(configuration, logger);
        ValidateHttpClientSettings(configuration, logger);
        ValidateUrlSettings(configuration, logger);
    }

    private static void ValidateCertificateSettings(IConfiguration configuration, ILogger logger)
    {
        // Check for certificate validation bypass
        var validateCertificatesSettings = configuration.GetSection("Client")
            .GetChildren()
            .Where(section => section.GetValue<bool?>("ValidateCertificates") == false);

        foreach (var setting in validateCertificatesSettings)
        {
            logger.LogWarning("Certificate validation is disabled for {ClientName}. This should only be used in development environments.", 
                setting.Key);
        }

        // Check for certificate revocation settings
        var revocationSettings = configuration.GetSection("Certificates")
            .GetChildren()
            .Where(section => section.GetValue<string>("RevocationMode") == "NoCheck");

        foreach (var setting in revocationSettings)
        {
            logger.LogWarning("Certificate revocation checking is disabled for {CertificateName}. Consider enabling for production environments.", 
                setting.Key);
        }
    }

    private static void ValidateHttpClientSettings(IConfiguration configuration, ILogger logger)
    {
        // Check for HTTP URLs in production-like environments
        var httpUrls = configuration.AsEnumerable()
            .Where(kvp => kvp.Key.Contains("Url", StringComparison.OrdinalIgnoreCase) && 
                         kvp.Value?.StartsWith("http://", StringComparison.OrdinalIgnoreCase) == true);

        foreach (var httpUrl in httpUrls)
        {
            logger.LogWarning("HTTP URL detected in configuration: {ConfigKey}. Consider using HTTPS for secure communication.", 
                httpUrl.Key);
        }
    }

    private static void ValidateUrlSettings(IConfiguration configuration, ILogger logger)
    {
        var urlSettings = configuration.AsEnumerable()
            .Where(kvp => kvp.Key.Contains("Url", StringComparison.OrdinalIgnoreCase) && 
                         !string.IsNullOrEmpty(kvp.Value));

        foreach (var urlSetting in urlSettings)
        {
            if (!SecurityUtilities.IsUrlSafe(urlSetting.Value))
            {
                logger.LogWarning("Potentially unsafe URL detected in configuration: {ConfigKey}. URL validation failed.", 
                    urlSetting.Key);
            }
        }
    }
}
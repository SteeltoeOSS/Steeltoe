// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Options;

namespace Steeltoe.Common.Security;

public sealed class ConfigureNamedCertificateOptions : IConfigureNamedOptions<CertificateOptions>
{
    private readonly IConfiguration _configuration;

    public ConfigureNamedCertificateOptions(IConfiguration configuration)
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
        ArgumentGuard.NotNull(options);

        options.Name = name;

        string certPath = _configuration[$"{CertificateOptions.ConfigurationPrefix}:{name}:certificate"];

        if (string.IsNullOrEmpty(certPath))
        {
            return;
        }

        options.Certificate = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? new X509Certificate2(certPath)
            : new X509Certificate2(certPath, string.Empty, X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);
    }
}

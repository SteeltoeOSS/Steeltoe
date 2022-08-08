// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Options;

namespace Steeltoe.Common.Security;

public class ConfigureCertificateOptions : IConfigureNamedOptions<CertificateOptions>
{
    private readonly IConfiguration _config;

    public ConfigureCertificateOptions(IConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public void Configure(string name, CertificateOptions options)
    {
        ArgumentGuard.NotNull(options);

        options.Name = name;

        string certPath = _config["certificate"];

        if (string.IsNullOrEmpty(certPath))
        {
            return;
        }

        options.Certificate = new X509Certificate2(certPath, string.Empty, X509KeyStorageFlags.EphemeralKeySet);
    }

    public void Configure(CertificateOptions options)
    {
        Configure(Microsoft.Extensions.Options.Options.DefaultName, options);
    }
}

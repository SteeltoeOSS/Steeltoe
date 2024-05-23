// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Common.Security;

public class CertificateSource : ICertificateSource
{
    private readonly string _certFilePath;

    public Type OptionsConfigurer => typeof(ConfigureCertificateOptions);

    public CertificateSource(string certFilePath)
    {
        _certFilePath = Path.GetFullPath(certFilePath);
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        if (!File.Exists(_certFilePath))
        {
            throw new InvalidOperationException($"Required certificate file not found:{_certFilePath}");
        }

        var certSource = new FileSource("certificate")
        {
            FileProvider = null,
            Path = Path.GetFileName(_certFilePath),
            Optional = false,
            ReloadOnChange = true,
            ReloadDelay = 1000,
            BasePath = Path.GetDirectoryName(_certFilePath)
        };

        return new CertificateProvider(certSource);
    }
}

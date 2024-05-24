// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Options;

namespace Steeltoe.Common.Security;

internal sealed class CertificateSource(string certificateName, string certificateFilePath) : ICertificateSource
{
    private readonly string _certificateName = certificateName;
    private readonly string _certFilePath = Path.GetFullPath(certificateFilePath);

    public Type OptionsConfigurer => typeof(ConfigureNamedCertificateOptions);

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        if (!File.Exists(_certFilePath))
        {
            throw new InvalidOperationException($"Required certificate file not found:{_certFilePath}");
        }

        var keyPrefix = CertificateOptions.ConfigurationPrefix + ConfigurationPath.KeyDelimiter + _certificateName + ConfigurationPath.KeyDelimiter;

        var certSource = new FileSource(keyPrefix + "certificate")
        {
            FileProvider = null,
            Path = Path.GetFileName(_certFilePath),
            Optional = false,
            ReloadOnChange = true,
            ReloadDelay = 1000,
            BasePath = Path.GetDirectoryName(_certFilePath)
        };

        return new CertificateProvider(_certificateName, certSource);
    }
}

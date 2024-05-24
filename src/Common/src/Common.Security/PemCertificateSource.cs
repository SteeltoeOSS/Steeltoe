// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Options;

namespace Steeltoe.Common.Security;

internal sealed class PemCertificateSource : ICertificateSource
{
    private readonly string _certificateName;
    private readonly string _certificateFilePath;
    private readonly string _keyFilePath;

    public Type OptionsConfigurer => typeof(PemConfigureCertificateOptions);

    public PemCertificateSource(string certificateName, string certFilePath, string keyFilePath)
    {
        _certificateName = certificateName;
        _certificateFilePath = Path.GetFullPath(certFilePath);
        _keyFilePath = Path.GetFullPath(keyFilePath);
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        string keyPrefix = CertificateOptions.ConfigurationPrefix + ConfigurationPath.KeyDelimiter + _certificateName + ConfigurationPath.KeyDelimiter;

        var certSource = new FileSource(keyPrefix + "certificate")
        {
            FileProvider = null,
            Path = Path.GetFileName(_certificateFilePath),
            Optional = false,
            ReloadOnChange = true,
            ReloadDelay = 1000
        };

        var keySource = new FileSource(keyPrefix + "privateKey")
        {
            FileProvider = null,
            Path = Path.GetFileName(_keyFilePath),
            Optional = false,
            ReloadOnChange = true,
            ReloadDelay = 1000
        };

        IConfigurationRoot certProvider = new ConfigurationBuilder().SetBasePath(Path.GetDirectoryName(_certificateFilePath)).Add(certSource).Build();

        IConfigurationRoot keyProvider = new ConfigurationBuilder().SetBasePath(Path.GetDirectoryName(_keyFilePath)).Add(keySource).Build();

        return new PemCertificateProvider(certProvider, keyProvider);
    }
}

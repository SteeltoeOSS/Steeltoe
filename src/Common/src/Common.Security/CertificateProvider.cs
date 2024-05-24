// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Options;

namespace Steeltoe.Common.Security;

internal sealed class CertificateProvider : ConfigurationProvider
{
    private readonly IConfigurationProvider _certificateProvider;
    private readonly string _certificateName;
    private readonly string _certificatePath;

    internal CertificateProvider(string certificateName, FileSource certificateSource)
    {
        _certificateName = certificateName;
        _certificateProvider = certificateSource.Build(new ConfigurationBuilder());
        _certificateProvider.GetReloadToken().RegisterChangeCallback(NotifyCertChanged, null);
        _certificatePath = Path.Combine(certificateSource.BasePath, certificateSource.Path);
    }

    public override void Load()
    {
        // for future use
    }

    public override bool TryGet(string key, out string value)
    {
        value = null;

        if (key.Equals($"{CertificateOptions.ConfigurationPrefix}:{_certificateName}:certificate", StringComparison.InvariantCultureIgnoreCase))
        {
            value = _certificatePath;
        }

        return !string.IsNullOrEmpty(value);
    }

    private void NotifyCertChanged(object state)
    {
        OnReload();
        _certificateProvider.GetReloadToken().RegisterChangeCallback(NotifyCertChanged, null);
    }
}

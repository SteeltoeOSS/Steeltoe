// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Certificates;

namespace Steeltoe.Security.Authorization.Certificate.Test;

internal static class ServiceProviderCertificateExtensions
{
    /// <summary>
    /// Gets the app instance identity certificate that is created from configuration. Tests dispose it, because on some platforms (such as Windows and
    /// macOS) private key material is left on disk when a certificate is not disposed before the process exits.
    /// </summary>
    public static X509Certificate2? GetAppInstanceIdentityCertificate(this IServiceProvider services)
    {
        var optionsMonitor = services.GetRequiredService<IOptionsMonitor<CertificateOptions>>();
        return optionsMonitor.Get(CertificateConfigurationExtensions.AppInstanceIdentityCertificateName).Certificate;
    }
}

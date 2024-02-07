// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Options;

namespace Steeltoe.Common.Http;

public sealed class ClientCertificatePrimaryHttpClientHandlerConfigurer : IPrimaryHttpClientHandlerConfigurer
{
    private readonly IOptionsMonitor<CertificateOptions> _optionsMonitor;

    public ClientCertificatePrimaryHttpClientHandlerConfigurer(IOptionsMonitor<CertificateOptions> optionsMonitor)
    {
        ArgumentGuard.NotNull(optionsMonitor);

        _optionsMonitor = optionsMonitor;
    }

    public void Configure(HttpClientHandler handler)
    {
        ArgumentGuard.NotNull(handler);

        X509Certificate2 certificate = _optionsMonitor.CurrentValue.Certificate;

        if (certificate != null)
        {
            handler.ClientCertificates.Add(certificate);
        }
    }
}

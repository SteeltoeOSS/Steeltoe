// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Configuration;

namespace Steeltoe.Common.Http.HttpClientPooling;

/// <summary>
/// Configures the primary <see cref="HttpClientHandler" /> for a named <see cref="HttpClient" /> by using a client certificate from configuration.
/// </summary>
public sealed class ClientCertificateHttpClientHandlerConfigurer : IHttpClientHandlerConfigurer
{
    private readonly IOptionsMonitor<CertificateOptions> _optionsMonitor;
    private string _clientCertificateName;

    public ClientCertificateHttpClientHandlerConfigurer(IOptionsMonitor<CertificateOptions> optionsMonitor)
    {
        ArgumentGuard.NotNull(optionsMonitor);

        _optionsMonitor = optionsMonitor;
    }

    public void SetCertificateName(string certificateName)
    {
        _clientCertificateName = certificateName;
    }

    public void Configure(HttpClientHandler handler)
    {
        ArgumentGuard.NotNull(handler);

        X509Certificate2 certificate = _optionsMonitor.Get(_clientCertificateName).Certificate;

        if (certificate != null)
        {
            handler.ClientCertificates.Add(certificate);
        }
    }
}

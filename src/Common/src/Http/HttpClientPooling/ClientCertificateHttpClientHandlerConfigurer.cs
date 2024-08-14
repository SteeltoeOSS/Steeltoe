// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Certificates;

namespace Steeltoe.Common.Http.HttpClientPooling;

/// <summary>
/// Configures the primary <see cref="HttpClientHandler" /> for a named <see cref="HttpClient" /> to send a client certificate obtained from options.
/// </summary>
internal sealed class ClientCertificateHttpClientHandlerConfigurer
{
    private readonly IOptionsMonitor<CertificateOptions> _optionsMonitor;

    public ClientCertificateHttpClientHandlerConfigurer(IOptionsMonitor<CertificateOptions> optionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        _optionsMonitor = optionsMonitor;
    }

    /// <summary>
    /// Configures the specified <see cref="HttpClientHandler" /> from options with the specified name. Falls back to unnamed options when no certificate is
    /// available.
    /// </summary>
    /// <param name="name">
    /// The name of the certificate options in configuration, or an empty string to use default options.
    /// </param>
    /// <param name="handler">
    /// The handler to configure.
    /// </param>
    public void Configure(string name, HttpClientHandler handler)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(handler);

        X509Certificate2? certificate = _optionsMonitor.Get(name).Certificate ?? _optionsMonitor.Get(Options.DefaultName).Certificate;

        if (certificate != null)
        {
            handler.ClientCertificates.Add(certificate);
        }
    }
}

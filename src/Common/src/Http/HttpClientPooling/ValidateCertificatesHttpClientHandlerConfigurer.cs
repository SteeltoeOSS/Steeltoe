// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;

namespace Steeltoe.Common.Http.HttpClientPooling;

/// <summary>
/// Configures the primary <see cref="HttpClientHandler" /> for a named <see cref="HttpClient" /> by turning certification validation off, based on
/// configuration.
/// </summary>
/// <typeparam name="TOptions">
/// The options type providing the configured value.
/// </typeparam>
internal sealed class ValidateCertificatesHttpClientHandlerConfigurer<TOptions> : IHttpClientHandlerConfigurer
    where TOptions : IValidateCertificatesOptions
{
    private readonly IOptionsMonitor<TOptions> _optionsMonitor;

    public ValidateCertificatesHttpClientHandlerConfigurer(IOptionsMonitor<TOptions> optionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        _optionsMonitor = optionsMonitor;
    }

    public void Configure(HttpClientHandler handler)
    {
        TOptions options = _optionsMonitor.CurrentValue;

        if (!options.ValidateCertificates)
        {
#pragma warning disable S4830 // Server certificates should be verified during SSL/TLS connections
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#pragma warning restore S4830 // Server certificates should be verified during SSL/TLS connections
        }
    }
}

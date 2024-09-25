// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.HttpExchanges;

internal sealed class HttpExchangesEndpointHandler : IHttpExchangesEndpointHandler
{
    private readonly IOptionsMonitor<HttpExchangesEndpointOptions> _optionsMonitor;
    private readonly IHttpExchangesRepository _httpExchangesRepository;
    private readonly ILogger<HttpExchangesEndpointHandler> _logger;

    public EndpointOptions Options => _optionsMonitor.CurrentValue;

    public HttpExchangesEndpointHandler(IOptionsMonitor<HttpExchangesEndpointOptions> optionsMonitor, IHttpExchangesRepository httpExchangesRepository,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(httpExchangesRepository);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _optionsMonitor = optionsMonitor;
        _httpExchangesRepository = httpExchangesRepository;
        _logger = loggerFactory.CreateLogger<HttpExchangesEndpointHandler>();
    }

    public Task<HttpExchangesResult> InvokeAsync(object? argument, CancellationToken cancellationToken)
    {
        _logger.LogTrace("Fetching Http Exchanges");
        HttpExchangesResult result = _httpExchangesRepository.GetHttpExchanges();
        return Task.FromResult(result);
    }
}

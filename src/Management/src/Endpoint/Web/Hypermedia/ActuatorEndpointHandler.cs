// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Web.Hypermedia;

/// <summary>
/// Actuator Endpoint provider the hypermedia link collection for all registered and enabled actuators.
/// </summary>
internal sealed class ActuatorEndpointHandler : IActuatorEndpointHandler
{
    private readonly ILogger<ActuatorEndpointHandler> _logger;
    private readonly IOptionsMonitor<HypermediaEndpointOptions> _options;
    private readonly IOptionsMonitor<ManagementEndpointOptions> _managementOption;
    private readonly IEnumerable<HttpMiddlewareOptions> _endpointOptions;

    public HttpMiddlewareOptions Options => _options.CurrentValue;

    public ActuatorEndpointHandler(IOptionsMonitor<HypermediaEndpointOptions> options, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        IEnumerable<HttpMiddlewareOptions> endpointOptions, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(managementOptions);
        ArgumentGuard.NotNull(endpointOptions);
        ArgumentGuard.NotNull(loggerFactory);

        _options = options;
        _managementOption = managementOptions;
        _endpointOptions = endpointOptions;
        _logger = loggerFactory.CreateLogger<ActuatorEndpointHandler>();
    }

    public Task<Links> InvokeAsync(string baseUrl, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(baseUrl);

        var service = new HypermediaService(_managementOption, _options, _endpointOptions, _logger);
        Links result = service.Invoke(baseUrl);
        return Task.FromResult(result);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

internal sealed class CloudFoundryEndpoint : ICloudFoundryEndpoint
{
    private readonly IOptionsMonitor<CloudFoundryHttpMiddlewareOptions> _options;
    private readonly IOptionsMonitor<ManagementEndpointOptions> _managementOptions;
    private readonly IEnumerable<IHttpMiddlewareOptions> _endpointOptions;
    private readonly ILogger<CloudFoundryEndpoint> _logger;

    public CloudFoundryEndpoint(IOptionsMonitor<CloudFoundryHttpMiddlewareOptions> options, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        IEnumerable<IHttpMiddlewareOptions> endpointOptions, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(managementOptions);
        ArgumentGuard.NotNull(endpointOptions);
        ArgumentGuard.NotNull(loggerFactory);

        _options = options;
        _managementOptions = managementOptions;
        _endpointOptions = endpointOptions;
        _logger = loggerFactory.CreateLogger<CloudFoundryEndpoint>();
    }

    public Task<Links> InvokeAsync(string baseUrl, CancellationToken cancellationToken)
    {
        var hypermediaService = new HypermediaService(_managementOptions, _options, _endpointOptions, _logger);
        return Task.Run(() => hypermediaService.Invoke(baseUrl), cancellationToken);
    }
}

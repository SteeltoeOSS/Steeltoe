// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.Web.Hypermedia;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

/// <summary>
/// CloudFoundryEndpointHandler provides hypermedia: a page is added with links to all the endpoints that are enabled. When deployed to CloudFoundry this
/// EndpointHandler is used for apps manager integration when <see cref="CloudFoundrySecurityMiddleware" /> is added.
/// </summary>
internal sealed class CloudFoundryEndpointHandler : ICloudFoundryEndpointHandler
{
    private readonly IOptionsMonitor<ManagementEndpointOptions> _managementOptions;
    private readonly IOptionsMonitor<CloudFoundryEndpointOptions> _options;
    private readonly IEnumerable<HttpMiddlewareOptions> _endpointOptions;
    private readonly ILogger<CloudFoundryEndpointHandler> _logger;

    public HttpMiddlewareOptions Options => _options.CurrentValue;

    public CloudFoundryEndpointHandler(IOptionsMonitor<ManagementEndpointOptions> managementOptions, IOptionsMonitor<CloudFoundryEndpointOptions> options,
        IEnumerable<HttpMiddlewareOptions> endpointOptions, ILogger<CloudFoundryEndpointHandler> logger)
    {
        ArgumentGuard.NotNull(managementOptions);
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(endpointOptions);
        ArgumentGuard.NotNull(logger);

        _managementOptions = managementOptions;
        _options = options;
        _endpointOptions = endpointOptions;
        _logger = logger;
    }

    public Task<Links> InvokeAsync(string baseUrl, CancellationToken cancellationToken)
    {
        var hypermediaService = new HypermediaService(_managementOptions, _options, _endpointOptions, _logger);
        return Task.Run(() => hypermediaService.Invoke(baseUrl), cancellationToken);
    }
}

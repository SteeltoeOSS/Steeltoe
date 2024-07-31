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
    private readonly IOptionsMonitor<ManagementOptions> _managementOptionsMonitor;
    private readonly IOptionsMonitor<CloudFoundryEndpointOptions> _endpointOptionsMonitor;
    private readonly ICollection<EndpointOptions> _endpointOptionsCollection;
    private readonly ILogger<HypermediaService> _hypermediaServiceLogger;

    public EndpointOptions Options => _endpointOptionsMonitor.CurrentValue;

    public CloudFoundryEndpointHandler(IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        IOptionsMonitor<CloudFoundryEndpointOptions> endpointOptionsMonitor, IEnumerable<EndpointOptions> endpointOptionsCollection,
        ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(managementOptionsMonitor);
        ArgumentGuard.NotNull(endpointOptionsMonitor);
        ArgumentGuard.NotNull(endpointOptionsCollection);
        ArgumentGuard.NotNull(loggerFactory);

        _managementOptionsMonitor = managementOptionsMonitor;
        _endpointOptionsMonitor = endpointOptionsMonitor;
        _endpointOptionsCollection = endpointOptionsCollection.ToList();
        _hypermediaServiceLogger = loggerFactory.CreateLogger<HypermediaService>();
    }

    public async Task<Links> InvokeAsync(string baseUrl, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(baseUrl);

        var hypermediaService = new HypermediaService(_managementOptionsMonitor, _endpointOptionsMonitor, _endpointOptionsCollection, _hypermediaServiceLogger);
        Links result = hypermediaService.Invoke(baseUrl);
        return await Task.FromResult(result);
    }
}

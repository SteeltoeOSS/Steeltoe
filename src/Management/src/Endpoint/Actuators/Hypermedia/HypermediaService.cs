// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Hypermedia;

internal sealed class HypermediaService
{
    private readonly IOptionsMonitor<ManagementOptions> _managementOptionsMonitor;
    private readonly EndpointOptions _endpointOptions;
    private readonly ICollection<IEndpointOptionsMonitorProvider> _endpointOptionsMonitorProviders;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<HypermediaService> _logger;

    public HypermediaService(IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        IOptionsMonitor<HypermediaEndpointOptions> hypermediaEndpointOptionsMonitor,
        ICollection<IEndpointOptionsMonitorProvider> endpointOptionsMonitorProviders, IHttpContextAccessor httpContextAccessor,
        ILogger<HypermediaService> logger)
    {
        ArgumentNullException.ThrowIfNull(managementOptionsMonitor);
        ArgumentNullException.ThrowIfNull(hypermediaEndpointOptionsMonitor);
        ArgumentNullException.ThrowIfNull(endpointOptionsMonitorProviders);
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        ArgumentGuard.ElementsNotNull(endpointOptionsMonitorProviders);
        ArgumentNullException.ThrowIfNull(logger);

        _managementOptionsMonitor = managementOptionsMonitor;
        _endpointOptions = hypermediaEndpointOptionsMonitor.CurrentValue;
        _endpointOptionsMonitorProviders = endpointOptionsMonitorProviders;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public HypermediaService(IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        IOptionsMonitor<CloudFoundryEndpointOptions> cloudFoundryEndpointOptionsMonitor,
        ICollection<IEndpointOptionsMonitorProvider> endpointOptionsMonitorProviders, IHttpContextAccessor httpContextAccessor,
        ILogger<HypermediaService> logger)
    {
        ArgumentNullException.ThrowIfNull(managementOptionsMonitor);
        ArgumentNullException.ThrowIfNull(cloudFoundryEndpointOptionsMonitor);
        ArgumentNullException.ThrowIfNull(endpointOptionsMonitorProviders);
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        ArgumentGuard.ElementsNotNull(endpointOptionsMonitorProviders);
        ArgumentNullException.ThrowIfNull(logger);

        _managementOptionsMonitor = managementOptionsMonitor;
        _endpointOptions = cloudFoundryEndpointOptionsMonitor.CurrentValue;
        _endpointOptionsMonitorProviders = endpointOptionsMonitorProviders;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public Links Invoke(Uri baseUrl)
    {
        ArgumentNullException.ThrowIfNull(baseUrl);

        var links = new Links();
        ManagementOptions managementOptions = _managementOptionsMonitor.CurrentValue;

        if (!_endpointOptions.IsEnabled(managementOptions))
        {
            return links;
        }

        _logger.LogTrace("Processing hypermedia for {ManagementOptions}", managementOptions);

        Link? selfLink = null;
        bool skipExposureCheck = PermissionsProvider.IsCloudFoundryRequest(baseUrl.PathAndQuery);
        string? basePath = managementOptions.GetBasePath(baseUrl.AbsolutePath);

        if (_httpContextAccessor.HttpContext?.Request != null)
        {
            basePath = $"{_httpContextAccessor.HttpContext.Request.PathBase}{basePath}";
        }

        foreach (EndpointOptions endpointOptions in _endpointOptionsMonitorProviders.Select(provider => provider.Get()).OrderBy(options => options.Id))
        {
            if (endpointOptions.Id == null || !endpointOptions.IsEnabled(managementOptions))
            {
                continue;
            }

            if (!skipExposureCheck && !endpointOptions.IsExposed(managementOptions))
            {
                continue;
            }

            if (endpointOptions.Id == _endpointOptions.Id)
            {
                selfLink = CreateLink(baseUrl, basePath, endpointOptions);
            }
            else
            {
                if (links.Entries.ContainsKey(endpointOptions.Id))
                {
                    _logger.LogWarning("Duplicate endpoint with ID '{DuplicateEndpointId}' detected.", endpointOptions.Id);
                }
                else
                {
                    Link link = CreateLink(baseUrl, basePath, endpointOptions);
                    links.Entries.Add(endpointOptions.Id, link);
                }
            }
        }

        if (selfLink != null)
        {
            links.Entries.Add("self", selfLink);
        }

        return links;
    }

    private static Link CreateLink(Uri baseUrl, string? basePath, EndpointOptions endpointOptions)
    {
        var builder = new UriBuilder(baseUrl)
        {
            Path = endpointOptions.GetEndpointPath(basePath)
        };

        string href = builder.Uri.ToString();
        return new Link(href, false);
    }
}

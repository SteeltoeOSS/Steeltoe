// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Hypermedia;

public class HypermediaService
{
    private readonly IEnumerable<IEndpointOptions> _endpointOptions;
    private readonly ILogger _logger;
    private readonly ManagementEndpointOptions _managementOptions;
    private readonly EndpointOptionsBase _options;

    public HypermediaService(IOptionsMonitor<ManagementEndpointOptions> managementOptions, IOptionsMonitor<HypermediaEndpointOptions> options,
        IEnumerable<IEndpointOptions> endpointOptions, ILogger logger)
    {
        ArgumentGuard.NotNull(managementOptions);
        ArgumentGuard.NotNull(options);

        _logger = logger;
        _managementOptions = managementOptions.Get(ActuatorContext.Name);
        _endpointOptions = endpointOptions;
        _options = options.CurrentValue;
    }

    public HypermediaService(IOptionsMonitor<ManagementEndpointOptions> managementOptions, IOptionsMonitor<CloudFoundryEndpointOptions> options,
        IEnumerable<IEndpointOptions> endpointOptions, ILogger logger)
    {
        ArgumentGuard.NotNull(managementOptions);
        ArgumentGuard.NotNull(options);
        _endpointOptions = endpointOptions;
        _logger = logger;
        _options = options.CurrentValue;
        _managementOptions = managementOptions.Get(CFContext.Name);
    }

    public Links Invoke(string baseUrl)
    {
        var links = new Links();

        if (!_options.IsEnabled(_managementOptions))
        {
            return links;
        }

        _logger.LogTrace("Processing hypermedia for {ManagementOptions}", _managementOptions);

        Link selfLink = null;

        foreach (IEndpointOptions opt in _endpointOptions)
        {
            if (!opt.IsEnabled(_managementOptions) || !opt.IsExposed(_managementOptions))
            {
                continue;
            }

            if (opt.Id == _options.Id)
            {
                selfLink = new Link(baseUrl);
            }
            else
            {
                if (!string.IsNullOrEmpty(opt.Id))
                {
                    if (!links._links.ContainsKey(opt.Id))
                    {
                        string linkPath = $"{baseUrl.TrimEnd('/')}/{opt.Path}";
                        links._links.Add(opt.Id, new Link(linkPath));
                    }
                    else if (links._links.ContainsKey(opt.Id))
                    {
                        _logger.LogWarning("Duplicate endpoint id detected: {DuplicateEndpointId}", opt.Id);
                    }
                }
            }
        }

        if (selfLink != null)
        {
            links._links.Add("self", selfLink);
        }

        return links;
    }
}

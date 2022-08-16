// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Hypermedia;

public class HypermediaService
{
    private readonly ILogger _logger;
    private readonly IManagementOptions _managementOptions;
    private readonly IEndpointOptions _options;

    public HypermediaService(IManagementOptions managementOptions, IEndpointOptions options, ILogger logger = null)
    {
        ArgumentGuard.NotNull(managementOptions);
        ArgumentGuard.NotNull(options);

        _logger = logger;
        _managementOptions = managementOptions;
        _options = options;
    }

    public Links Invoke(string baseUrl)
    {
        IList<IEndpointOptions> endpointOptions = _managementOptions.EndpointOptions;
        var links = new Links();

        if (!_options.IsEnabled(_managementOptions))
        {
            return links;
        }

        _logger?.LogTrace("Processing hypermedia for {ManagementOptions}", _managementOptions);

        foreach (IEndpointOptions opt in endpointOptions)
        {
            if (!opt.IsEnabled(_managementOptions) || !opt.IsExposed(_managementOptions))
            {
                continue;
            }

            if (opt == _options)
            {
                links._links.Add("self", new Link(baseUrl));
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
                        _logger?.LogWarning("Duplicate endpoint id detected: {DuplicateEndpointId}", opt.Id);
                    }
                }
            }
        }

        return links;
    }
}

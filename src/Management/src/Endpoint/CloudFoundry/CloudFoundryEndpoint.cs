// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

public class CloudFoundryEndpoint : ICloudFoundryEndpoint
{
    private readonly IOptionsMonitor<CloudFoundryEndpointOptions> _options;
    private readonly IOptionsMonitor<ManagementEndpointOptions> _managementOptions;
    private readonly IEnumerable<IEndpointOptions> _endpointOptions;
    private readonly ILogger<CloudFoundryEndpoint> _logger;

    public IEndpointOptions Options => _options.CurrentValue;

    public CloudFoundryEndpoint(IOptionsMonitor<CloudFoundryEndpointOptions> options, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        IEnumerable<IEndpointOptions> endpointOptions, ILoggerFactory loggerFactory)
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

    public virtual Links Invoke(string baseUrl)
    {
        var hypermediaService = new HypermediaService(_managementOptions, _options, _endpointOptions, _logger);
        return hypermediaService.Invoke(baseUrl);
    }
}

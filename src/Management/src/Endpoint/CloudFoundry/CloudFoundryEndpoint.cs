// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

public class CloudFoundryEndpoint : IEndpoint<Links, string>, ICloudFoundryEndpoint
{
    private readonly IOptionsMonitor<CloudFoundryEndpointOptions> _options;
    private readonly IOptionsMonitor<ManagementEndpointOptions> _managementOptions;
    private readonly ILogger<CloudFoundryEndpoint> _logger;
  //  private readonly IManagementOptions _managementOptions;

    //protected new ICloudFoundryOptions Options => options as ICloudFoundryOptions;

    public CloudFoundryEndpoint(IOptionsMonitor<CloudFoundryEndpointOptions> options, IOptionsMonitor<ManagementEndpointOptions> managementOptions, ILogger<CloudFoundryEndpoint> logger = null)
       // : base(options)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(managementOptions);
        _options = options;
        _managementOptions = managementOptions;
        _logger = logger;
    }

    public IEndpointOptions Options => _options.CurrentValue;

    // public IOptionsMonitor<CloudFoundryEndpointOptions> Options => _options;

    public Links Invoke(string baseUrl)
    {
        var hypermediaService = new HypermediaService(_managementOptions, _options, _logger);
        return hypermediaService.Invoke(baseUrl);
    }
}

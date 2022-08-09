// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Hypermedia;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

public class CloudFoundryEndpoint : AbstractEndpoint<Links, string>, ICloudFoundryEndpoint
{
    private readonly ILogger<CloudFoundryEndpoint> _logger;
    private readonly IManagementOptions _managementOptions;

    protected new ICloudFoundryOptions Options => options as ICloudFoundryOptions;

    public CloudFoundryEndpoint(ICloudFoundryOptions options, CloudFoundryManagementOptions managementOptions, ILogger<CloudFoundryEndpoint> logger = null)
        : base(options)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(managementOptions);

        _managementOptions = managementOptions;
        _logger = logger;
    }

    public override Links Invoke(string baseUrl)
    {
        var hypermediaService = new HypermediaService(_managementOptions, options, _logger);
        return hypermediaService.Invoke(baseUrl);
    }
}

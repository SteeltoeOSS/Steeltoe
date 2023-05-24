// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.Web.Hypermedia;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

internal sealed class CloudFoundryEndpointHandler : ICloudFoundryEndpointHandler
{
    private HypermediaService _hypermediaService;

    public CloudFoundryEndpointHandler(HypermediaService hypermediaService, IOptionsMonitor<CloudFoundryEndpointOptions> options)
    {
        ArgumentGuard.NotNull(hypermediaService);
        ArgumentGuard.NotNull(options);
        _hypermediaService = hypermediaService;
        Options = options.CurrentValue;
    }

    public HttpMiddlewareOptions Options { get;  }

    public Task<Links> InvokeAsync(string baseUrl, CancellationToken cancellationToken)
    {
        return Task.Run(() => _hypermediaService.Invoke(baseUrl), cancellationToken);
    }
}

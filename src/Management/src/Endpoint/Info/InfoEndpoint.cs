// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Info;

namespace Steeltoe.Management.Endpoint.Info;

public class InfoEndpoint :  IInfoEndpoint
{
    private readonly IList<IInfoContributor> _contributors;
    private readonly IOptionsMonitor<InfoEndpointOptions> _options;
    private readonly ILogger<InfoEndpoint> _logger;

    
    public InfoEndpoint(IOptionsMonitor<InfoEndpointOptions> options, IEnumerable<IInfoContributor> contributors, ILogger<InfoEndpoint> logger = null)
    {
        _options = options;
        _logger = logger;
        _contributors = contributors.ToList();
    }
    public IEndpointOptions Options => _options.CurrentValue;

    public Dictionary<string, object> Invoke()
    {
        return BuildInfo(_contributors);
    }

    protected virtual Dictionary<string, object> BuildInfo(IList<IInfoContributor> infoContributors)
    {
        IInfoBuilder builder = new InfoBuilder();

        foreach (IInfoContributor contributor in infoContributors)
        {
            try
            {
                contributor.Contribute(builder);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Operation failed.");
            }
        }

        return builder.Build();
    }
}

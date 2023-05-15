// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Info;

namespace Steeltoe.Management.Endpoint.Info;

internal sealed class InfoEndpoint : IInfoEndpoint
{
    private readonly IList<IInfoContributor> _contributors;
    private readonly IOptionsMonitor<InfoEndpointOptions> _options;
    private readonly ILogger<InfoEndpoint> _logger;
    public IHttpMiddlewareOptions Options => _options.CurrentValue;

    public InfoEndpoint(IOptionsMonitor<InfoEndpointOptions> options, IEnumerable<IInfoContributor> contributors, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(contributors);
        ArgumentGuard.NotNull(loggerFactory);

        _options = options;
        _contributors = contributors.ToList();
        _logger = loggerFactory.CreateLogger<InfoEndpoint>();
    }

    public Task<Dictionary<string, object>> InvokeAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() => BuildInfo(_contributors), cancellationToken);
    }

    private Dictionary<string, object> BuildInfo(IList<IInfoContributor> infoContributors)
    {
        ArgumentGuard.NotNull(infoContributors);

        IInfoBuilder builder = new InfoBuilder();

        foreach (IInfoContributor contributor in infoContributors)
        {
            try
            {
                contributor.Contribute(builder);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Operation failed.");
            }
        }

        return builder.Build();
    }
}

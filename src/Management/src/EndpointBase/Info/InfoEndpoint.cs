// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Info;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Endpoint.Info;

public class InfoEndpoint : AbstractEndpoint<Dictionary<string, object>>, IInfoEndpoint
{
    private readonly IList<IInfoContributor> _contributors;
    private readonly ILogger<InfoEndpoint> _logger;

    public InfoEndpoint(IInfoOptions options, IEnumerable<IInfoContributor> contributors, ILogger<InfoEndpoint> logger = null)
        : base(options)
    {
        _logger = logger;
        _contributors = contributors.ToList();
    }

    public new IInfoOptions Options
    {
        get
        {
            return options as IInfoOptions;
        }
    }

    public override Dictionary<string, object> Invoke()
    {
        return BuildInfo(_contributors);
    }

    protected virtual Dictionary<string, object> BuildInfo(IList<IInfoContributor> infoContributors)
    {
        IInfoBuilder builder = new InfoBuilder();
        foreach (var contributor in infoContributors)
        {
            try
            {
                contributor.Contribute(builder);
            }
            catch (Exception e)
            {
                _logger?.LogError("Exception: {0}", e);
            }
        }

        return builder.Build();
    }
}

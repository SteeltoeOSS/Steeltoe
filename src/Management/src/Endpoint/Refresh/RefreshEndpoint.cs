// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Management.Endpoint.Refresh;

public class RefreshEndpoint : AbstractEndpoint<IList<string>>, IRefreshEndpoint
{
    private readonly ILogger<RefreshEndpoint> _logger;
    private readonly IConfiguration _configuration;

    public new IRefreshOptions Options => options as IRefreshOptions;

    public RefreshEndpoint(IRefreshOptions options, IConfiguration configuration, ILogger<RefreshEndpoint> logger = null)
        : base(options)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger;
    }

    public override IList<string> Invoke()
    {
        return DoInvoke(_configuration);
    }

    public IList<string> DoInvoke(IConfiguration configuration)
    {
        if (configuration is IConfigurationRoot root)
        {
            root.Reload();
        }

        if (!Options.ReturnConfiguration)
        {
            return new List<string>();
        }

        var keys = new List<string>();

        foreach (KeyValuePair<string, string> kvp in configuration.AsEnumerable())
        {
            keys.Add(kvp.Key);
        }

        return keys;
    }
}

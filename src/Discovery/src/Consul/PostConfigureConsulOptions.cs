// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Discovery.Consul.Configuration;

namespace Steeltoe.Discovery.Consul;

internal sealed partial class PostConfigureConsulOptions : IPostConfigureOptions<ConsulOptions>
{
    private readonly IOptionsMonitor<ConsulDiscoveryOptions> _optionsMonitor;
    private readonly ILogger<PostConfigureConsulOptions> _logger;

    public PostConfigureConsulOptions(IOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor, ILogger<PostConfigureConsulOptions> logger)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(logger);

        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public void PostConfigure(string? name, ConsulOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (_optionsMonitor.CurrentValue.Enabled && (Platform.IsContainerized || Platform.IsCloudHosted) && options.Host == "localhost")
        {
            LogLocalhostConsulUrl($"{options.Scheme}://{options.Host}:{options.Port}");
        }
    }

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning,
        Message = "Consul URL '{Url}' is unlikely to be valid in containerized or cloud environments. " +
            "Please configure Consul:Host with a non-localhost server.")]
    private partial void LogLocalhostConsulUrl(string url);
}

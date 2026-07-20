// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Discovery.Consul.Configuration;

namespace Steeltoe.Discovery.Consul;

internal sealed partial class ValidateConsulOptions : IValidateOptions<ConsulOptions>
{
    private readonly ILogger<ValidateConsulOptions> _logger;
    private readonly IOptionsMonitor<ConsulDiscoveryOptions> _discoveryOptionsMonitor;

    public ValidateConsulOptions(IOptionsMonitor<ConsulDiscoveryOptions> discoveryOptionsMonitor, ILogger<ValidateConsulOptions> logger)
    {
        ArgumentNullException.ThrowIfNull(discoveryOptionsMonitor);

        _discoveryOptionsMonitor = discoveryOptionsMonitor;
        _logger = logger;
    }

    public ValidateOptionsResult Validate(string? name, ConsulOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (_discoveryOptionsMonitor.CurrentValue.Enabled && (Platform.IsContainerized || Platform.IsCloudHosted) && options.Host == "localhost")
        {
            LogLocalhostConsulUrl(_logger, $"{options.Scheme}://{options.Host}:{options.Port}");
        }

        return ValidateOptionsResult.Success;
    }

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning,
        Message = "Consul URL '{Url}' is unlikely to be valid in containerized or cloud environments. " +
            "Please configure Consul:Host with a non-localhost server.")]
    private static partial void LogLocalhostConsulUrl(ILogger logger, string url);
}

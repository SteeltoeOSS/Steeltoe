// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Discovery.Consul.Configuration;

namespace Steeltoe.Discovery.Consul;

internal sealed class ValidateConsulOptions : IValidateOptions<ConsulOptions>
{
    private readonly IOptionsMonitor<ConsulDiscoveryOptions> _discoveryOptionsMonitor;

    public ValidateConsulOptions(IOptionsMonitor<ConsulDiscoveryOptions> discoveryOptionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(discoveryOptionsMonitor);

        _discoveryOptionsMonitor = discoveryOptionsMonitor;
    }

    public ValidateOptionsResult Validate(string? name, ConsulOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (_discoveryOptionsMonitor.CurrentValue.Enabled && (Platform.IsContainerized || Platform.IsCloudHosted) && options.Host == "localhost")
        {
            return ValidateOptionsResult.Fail(
                $"Consul URL '{options.Scheme}://{options.Host}:{options.Port}' is not valid in containerized or cloud environments. " +
                "Please configure Consul:Host with a non-localhost server.");
        }

        return ValidateOptionsResult.Success;
    }
}

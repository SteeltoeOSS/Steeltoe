// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common;

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding.PostProcessors;

internal sealed class IdentityCloudFoundryPostProcessor : CloudFoundryPostProcessor
{
    internal const string BindingType = "p-identity";
    internal const string AuthenticationConfigurationKeyPrefix = "Authentication:Schemes";

    internal static readonly string[] AuthenticationSchemes =
    [
        "OpenIdConnect",
        "Bearer"
    ];

    private readonly ILogger<IdentityCloudFoundryPostProcessor> _logger;

    public IdentityCloudFoundryPostProcessor(ILogger<IdentityCloudFoundryPostProcessor> logger)
    {
        ArgumentGuard.NotNull(logger);

        _logger = logger;
    }

    public override void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string?> configurationData)
    {
        bool hasMapped = false;

        foreach (string key in FilterKeys(configurationData, BindingType))
        {
            if (hasMapped)
            {
                _logger.LogWarning("Multiple identity service bindings found, which is not supported. Using the first binding from VCAP_SERVICES.");
                break;
            }

            var mapper = ServiceBindingMapper.Create(configurationData, key, BindingType, AuthenticationConfigurationKeyPrefix);

            foreach (string scheme in AuthenticationSchemes)
            {
                mapper.MapFromTo("credentials:auth_domain", $"{scheme}:Authority");
                mapper.MapFromTo("credentials:client_id", $"{scheme}:ClientId");
                mapper.MapFromTo("credentials:client_secret", $"{scheme}:ClientSecret");
            }

            hasMapped = true;
        }
    }
}

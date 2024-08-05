// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding.PostProcessors;

internal sealed class EurekaCloudFoundryPostProcessor : CloudFoundryPostProcessor
{
    internal const string BindingType = "eureka";
    internal const string EurekaConfigurationKeyPrefix = "eureka:client";
    private readonly ILogger<EurekaCloudFoundryPostProcessor> _logger;

    public EurekaCloudFoundryPostProcessor(ILogger<EurekaCloudFoundryPostProcessor> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    public override void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string?> configurationData)
    {
        bool hasMapped = false;

        foreach (string key in FilterKeys(configurationData, BindingType, KeyFilterSources.Tag))
        {
            if (hasMapped)
            {
                _logger.LogWarning("Multiple Eureka service bindings found, which is not supported. Using the first binding from VCAP_SERVICES.");
                break;
            }

            var mapper = ServiceBindingMapper.Create(configurationData, key, BindingType, EurekaConfigurationKeyPrefix);

            // Mapping from CloudFoundry service binding credentials to Eureka configuration keys.
            // There's no official documentation for the available credentials.

            string? serviceUri = mapper.GetFromValue("credentials:uri");

            if (serviceUri != null)
            {
                mapper.MapFromTo("credentials:uri", "ServiceUrl");
                mapper.SetToValue("ServiceUrl", serviceUri + "/eureka/");
            }

            mapper.MapFromTo("credentials:client_id", "ClientId");
            mapper.MapFromTo("credentials:client_secret", "ClientSecret");
            mapper.MapFromTo("credentials:access_token_uri", "AccessTokenUri");
            mapper.SetToValue("Enabled", "true");

            hasMapped = true;
        }
    }
}

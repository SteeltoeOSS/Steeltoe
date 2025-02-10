// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Configuration;

/// <summary>
/// Configures the Cross-Origin Resource Sharing (CORS) policy for actuator endpoints.
/// </summary>
internal sealed class ConfigureActuatorsCorsPolicyOptions : IConfigureOptions<CorsOptions>
{
    private readonly IOptionsMonitor<ActuatorsCorsPolicyOptions> _policyOptionsMonitor;
    private readonly IEndpointOptionsMonitorProvider[] _optionsMonitorProviderArray;

    public ConfigureActuatorsCorsPolicyOptions(IOptionsMonitor<ActuatorsCorsPolicyOptions> policyOptionsMonitor,
        IEnumerable<IEndpointOptionsMonitorProvider> endpointOptionsMonitorProviders)
    {
        ArgumentNullException.ThrowIfNull(policyOptionsMonitor);
        ArgumentNullException.ThrowIfNull(endpointOptionsMonitorProviders);

        IEndpointOptionsMonitorProvider[] optionsMonitorProviderArray = endpointOptionsMonitorProviders.ToArray();
        ArgumentGuard.ElementsNotNull(optionsMonitorProviderArray);

        _policyOptionsMonitor = policyOptionsMonitor;
        _optionsMonitorProviderArray = optionsMonitorProviderArray;
    }

    public void Configure(CorsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.AddPolicy(ActuatorsCorsPolicyOptions.PolicyName, policyBuilder =>
        {
            HashSet<string> methods = GetEndpointMethods();
            policyBuilder.WithMethods([.. methods]);

            if (Platform.IsCloudFoundry)
            {
                policyBuilder.WithHeaders("Authorization", "X-Cf-App-Instance", "Content-Type", "Content-Disposition");
            }

            ActuatorsCorsPolicyOptions policyOptions = _policyOptionsMonitor.CurrentValue;

            if (policyOptions.ConfigureActions.Count > 0)
            {
                foreach (Action<CorsPolicyBuilder> configureAction in policyOptions.ConfigureActions)
                {
                    configureAction(policyBuilder);
                }
            }
            else
            {
                policyBuilder.AllowAnyOrigin();
            }
        });
    }

    private HashSet<string> GetEndpointMethods()
    {
        return _optionsMonitorProviderArray.Select(provider => provider.Get()).SelectMany(endpointOptions => endpointOptions.GetSafeAllowedVerbs()).ToHashSet();
    }
}

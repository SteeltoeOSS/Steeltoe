// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq;

namespace Steeltoe.Bootstrap.Autoconfig;

public static class SteeltoeAssemblies
{
    public const string SteeltoeCircuitBreakerHystrixMetricsEventsCore = "Steeltoe.CircuitBreaker.Hystrix.MetricsEventsCore";
    public const string SteeltoeCircuitBreakerHystrixMetricsStreamCore = "Steeltoe.CircuitBreaker.Hystrix.MetricsStreamCore";
    public const string SteeltoeCircuitBreakerHystrixCore = "Steeltoe.CircuitBreaker.HystrixCore";
    public const string SteeltoeExtensionsConfigurationCloudFoundryBase = "Steeltoe.Extensions.Configuration.CloudFoundryBase";
    public const string SteeltoeExtensionsConfigurationCloudFoundryCore = "Steeltoe.Extensions.Configuration.CloudFoundryCore";
    public const string SteeltoeExtensionsConfigurationConfigServerBase = "Steeltoe.Extensions.Configuration.ConfigServerBase";
    public const string SteeltoeExtensionsConfigurationConfigServerCore = "Steeltoe.Extensions.Configuration.ConfigServerCore";
    public const string SteeltoeExtensionsConfigurationKubernetesBase = "Steeltoe.Extensions.Configuration.KubernetesBase";
    public const string SteeltoeExtensionsConfigurationKubernetesCore = "Steeltoe.Extensions.Configuration.KubernetesCore";
    public const string SteeltoeExtensionsConfigurationRandomValueBase = "Steeltoe.Extensions.Configuration.RandomValueBase";
    public const string SteeltoeExtensionsConfigurationPlaceholderBase = "Steeltoe.Extensions.Configuration.PlaceholderBase";
    public const string SteeltoeExtensionsConfigurationPlaceholderCore = "Steeltoe.Extensions.Configuration.PlaceholderCore";
    public const string SteeltoeConnectorEf6Core = "Steeltoe.Connector.EF6Core";
    public const string SteeltoeConnectorEfCore = "Steeltoe.Connector.EFCore";
    public const string SteeltoeConnectorConnectorCore = "Steeltoe.Connector.ConnectorCore";
    public const string SteeltoeDiscoveryClientBase = "Steeltoe.Discovery.ClientBase";
    public const string SteeltoeDiscoveryClientCore = "Steeltoe.Discovery.ClientCore";
    public const string SteeltoeExtensionsLoggingDynamicSerilogCore = "Steeltoe.Extensions.Logging.DynamicSerilogCore";
    public const string SteeltoeExtensionsLoggingDynamicLogger = "Steeltoe.Extensions.Logging.DynamicLogger";
    public const string SteeltoeManagementCloudFoundryCore = "Steeltoe.Management.CloudFoundryCore";
    public const string SteeltoeManagementEndpointCore = "Steeltoe.Management.EndpointCore";
    public const string SteeltoeManagementKubernetesCore = "Steeltoe.Management.KubernetesCore";
    public const string SteeltoeManagementTaskCore = "Steeltoe.Management.TaskCore";
    public const string SteeltoeManagementTracingBase = "Steeltoe.Management.TracingBase";
    public const string SteeltoeManagementTracingCore = "Steeltoe.Management.TracingCore";
    public const string SteeltoeSecurityAuthenticationCloudFoundryCore = "Steeltoe.Security.Authentication.CloudFoundryCore";
    public const string SteeltoeSecurityAuthenticationMtlsCore = "Steeltoe.Security.Authentication.MtlsCore";
    public const string SteeltoeSecurityDataProtectionCredHubCore = "Steeltoe.Security.DataProtection.CredHubCore";
    public const string SteeltoeSecurityDataProtectionRedisCore = "Steeltoe.Security.DataProtection.RedisCore";

    internal static readonly string[] AllAssemblies = typeof(SteeltoeAssemblies).GetFields()
        .Where(x => x.FieldType == typeof(string))
        .Select(x => x.GetValue(null))
        .Cast<string>()
        .ToArray();
}

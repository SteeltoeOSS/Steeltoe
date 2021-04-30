// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;

namespace Steeltoe.Bootstrap.Autoconfig
{
    public static class SteeltoeAssemblies
    {
        public const string Steeltoe_CircuitBreaker_Hystrix_MetricsEventsCore = "Steeltoe.CircuitBreaker.Hystrix.MetricsEventsCore";
        public const string Steeltoe_CircuitBreaker_Hystrix_MetricsStreamCore = "Steeltoe.CircuitBreaker.Hystrix.MetricsStreamCore";
        public const string Steeltoe_CircuitBreaker_HystrixCore = "Steeltoe.CircuitBreaker.HystrixCore";
        public const string Steeltoe_Extensions_Configuration_CloudFoundryCore = "Steeltoe.Extensions.Configuration.CloudFoundryCore";
        public const string Steeltoe_Extensions_Configuration_ConfigServerCore = "Steeltoe.Extensions.Configuration.ConfigServerCore";
        public const string Steeltoe_Extensions_Configuration_KubernetesCore = "Steeltoe.Extensions.Configuration.KubernetesCore";
        public const string Steeltoe_Extensions_Configuration_RandomValueBase = "Steeltoe.Extensions.Configuration.RandomValueBase";
        public const string Steeltoe_Extensions_Configuration_PlaceholderCore = "Steeltoe.Extensions.Configuration.PlaceholderCore";
        public const string Steeltoe_Connector_EF6Core = "Steeltoe.Connector.EF6Core";
        public const string Steeltoe_Connector_EFCore = "Steeltoe.Connector.EFCore";
        public const string Steeltoe_Connector_ConnectorCore = "Steeltoe.Connector.ConnectorCore";
        public const string Steeltoe_Discovery_ClientBase = "Steeltoe.Discovery.ClientBase";
        public const string Steeltoe_Discovery_ClientCore = "Steeltoe.Discovery.ClientCore";
        public const string Steeltoe_Extensions_Logging_DynamicSerilogCore = "Steeltoe.Extensions.Logging.DynamicSerilogCore";
        public const string Steeltoe_Extensions_Logging_DynamicLogger = "Steeltoe.Extensions.Logging.DynamicLogger";
        public const string Steeltoe_Management_CloudFoundryCore = "Steeltoe.Management.CloudFoundryCore";
        public const string Steeltoe_Management_EndpointCore = "Steeltoe.Management.EndpointCore";
        public const string Steeltoe_Management_KubernetesCore = "Steeltoe.Management.KubernetesCore";
        public const string Steeltoe_Management_TaskCore = "Steeltoe.Management.TaskCore";
        public const string Steeltoe_Management_TracingCore = "Steeltoe.Management.TracingCore";
        public const string Steeltoe_Security_Authentication_CloudFoundryCore = "Steeltoe.Security.Authentication.CloudFoundryCore";
        public const string Steeltoe_Security_Authentication_MtlsCore = "Steeltoe.Security.Authentication.MtlsCore";
        public const string Steeltoe_Security_DataProtection_CredHubCore = "Steeltoe.Security.DataProtection.CredHubCore";
        public const string Steeltoe_Security_DataProtection_RedisCore = "Steeltoe.Security.DataProtection.RedisCore";

        internal static readonly string[] AllAssemblies = typeof(SteeltoeAssemblies).GetFields()
            .Where(x => x.FieldType == typeof(string))
            .Select(x => x.GetValue(null))
            .Cast<string>()
            .ToArray();
    }
}
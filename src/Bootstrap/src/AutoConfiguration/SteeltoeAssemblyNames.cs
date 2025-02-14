// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Bootstrap.AutoConfiguration;

/// <summary>
/// Lists the names of Steeltoe assemblies that are used in autoconfiguration.
/// </summary>
public static class SteeltoeAssemblyNames
{
    public const string ConfigurationCloudFoundry = "Steeltoe.Configuration.CloudFoundry";
    public const string ConfigurationConfigServer = "Steeltoe.Configuration.ConfigServer";
    public const string ConfigurationRandomValue = "Steeltoe.Configuration.RandomValue";
    public const string ConfigurationSpringBoot = "Steeltoe.Configuration.SpringBoot";
    public const string ConfigurationPlaceholder = "Steeltoe.Configuration.Placeholder";
    public const string ConfigurationEncryption = "Steeltoe.Configuration.Encryption";
    public const string Connectors = "Steeltoe.Connectors";
    public const string DiscoveryConfiguration = "Steeltoe.Discovery.Configuration";
    public const string DiscoveryConsul = "Steeltoe.Discovery.Consul";
    public const string DiscoveryEureka = "Steeltoe.Discovery.Eureka";
    public const string LoggingDynamicConsole = "Steeltoe.Logging.DynamicConsole";
    public const string LoggingDynamicSerilog = "Steeltoe.Logging.DynamicSerilog";
    public const string ManagementEndpoint = "Steeltoe.Management.Endpoint";
    public const string ManagementPrometheus = "Steeltoe.Management.Prometheus";
    public const string ManagementTracing = "Steeltoe.Management.Tracing";

    internal static readonly IReadOnlySet<string> All = typeof(SteeltoeAssemblyNames).GetFields().Where(field => field.FieldType == typeof(string))
        .Select(field => field.GetValue(null)).Cast<string>().ToHashSet();

    internal static IReadOnlySet<string> Only(string assemblyName)
    {
        return All.Except([assemblyName]).ToHashSet();
    }
}

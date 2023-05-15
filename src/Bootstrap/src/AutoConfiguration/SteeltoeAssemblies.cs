// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

// ReSharper disable MemberCanBeInternal

namespace Steeltoe.Bootstrap.AutoConfiguration;

public static class SteeltoeAssemblies
{
    public const string SteeltoeExtensionsConfigurationCloudFoundry = "Steeltoe.Configuration.CloudFoundry";
    public const string SteeltoeExtensionsConfigurationConfigServer = "Steeltoe.Configuration.ConfigServer";
    public const string SteeltoeExtensionsConfigurationKubernetes = "Steeltoe.Configuration.Kubernetes";
    public const string SteeltoeExtensionsConfigurationRandomValue = "Steeltoe.Configuration.RandomValue";
    public const string SteeltoeExtensionsConfigurationPlaceholder = "Steeltoe.Configuration.Placeholder";
    public const string SteeltoeConnectors = "Steeltoe.Connectors";
    public const string SteeltoeDiscoveryClient = "Steeltoe.Discovery.Client";
    public const string SteeltoeExtensionsLoggingDynamicSerilog = "Steeltoe.Logging.DynamicSerilog";
    public const string SteeltoeManagementEndpoint = "Steeltoe.Management.Endpoint";
    public const string SteeltoeManagementKubernetes = "Steeltoe.Management.Kubernetes";
    public const string SteeltoeManagementTracing = "Steeltoe.Management.Tracing";
    public const string SteeltoeSecurityAuthenticationCloudFoundry = "Steeltoe.Security.Authentication.CloudFoundry";
    public const string SteeltoeWavefront = "Steeltoe.Management.Wavefront";
    public const string SteeltoePrometheus = "Steeltoe.Management.Prometheus";

    internal static readonly string[] AllAssemblies = typeof(SteeltoeAssemblies).GetFields().Where(x => x.FieldType == typeof(string))
        .Select(x => x.GetValue(null)).Cast<string>().ToArray();
}

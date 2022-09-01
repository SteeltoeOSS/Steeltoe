// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace Steeltoe.Common;

public static class Platform
{
    public const string VcapApplication = "VCAP_APPLICATION";
    public const string KubernetesHost = "KUBERNETES_SERVICE_HOST";

    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

#pragma warning disable S100 // Methods and properties should be named in PascalCase
    public static bool IsOSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
#pragma warning restore S100 // Methods and properties should be named in PascalCase

    /// <summary>
    /// Gets a value indicating whether or not the application appears to be running in a container.
    /// </summary>
    public static bool IsContainerized => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

    /// <summary>
    /// Gets a value indicating whether or not the platform is Cloud Foundry by checking if VCAP_APPLICATION has been set.
    /// </summary>
    public static bool IsCloudFoundry => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(VcapApplication));

    /// <summary>
    /// Gets a value indicating whether or not the platform is Kubernetes by checking if KUBERNETES_HOST has been set.
    /// </summary>
    public static bool IsKubernetes => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(KubernetesHost));

    /// <summary>
    /// Gets a value indicating whether an app is running on a cloud provider. Currently supports Cloud Foundry and Kubernetes.
    /// </summary>
    public static bool IsCloudHosted => IsCloudFoundry || IsKubernetes;
}

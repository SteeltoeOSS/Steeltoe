// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Steeltoe.Common
{
    public static class Platform
    {
        public const string NET_FRAMEWORK = ".NET Framework";
        public const string NET_CORE = ".NET Core";
        public const string VCAP_APPLICATION = "VCAP_APPLICATION";
        public const string KUBERNETES_HOST = "KUBERNETES_SERVICE_HOST";

        public static bool IsFullFramework => RuntimeInformation.FrameworkDescription.StartsWith(NET_FRAMEWORK);

        public static bool IsNetCore => RuntimeInformation.FrameworkDescription.StartsWith(NET_CORE);

        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsCloudFoundry => Environment.GetEnvironmentVariable(VCAP_APPLICATION) != null;

        public static bool IsKubernetes => Environment.GetEnvironmentVariable(KUBERNETES_HOST) != null;

        /// <summary>
        /// Gets a value indicating whether an app is running on a cloud provider. Currently supports Cloud Foundry and Kubernetes
        /// </summary>
        public static bool IsCloudHosted => IsCloudFoundry || IsKubernetes;
    }
}

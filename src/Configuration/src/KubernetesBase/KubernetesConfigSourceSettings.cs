// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Kubernetes;

namespace Steeltoe.Extensions.Configuration.Kubernetes
{
    internal class KubernetesConfigSourceSettings
    {
        internal KubernetesConfigSourceSettings(string @namespace, string name, ReloadSettings reloadSettings, ILogger logger = null)
        {
            Namespace = @namespace ?? "default";
            Name = name;
            ReloadSettings = reloadSettings;
            Logger = logger;
        }

        internal string Name { get; set; }

        internal string Namespace { get; set; }

        internal ReloadSettings ReloadSettings { get; set; }

        internal ILogger Logger { get; set; }
    }
}

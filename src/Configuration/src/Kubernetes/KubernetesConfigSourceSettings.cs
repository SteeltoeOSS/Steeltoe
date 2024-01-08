// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Kubernetes;

namespace Steeltoe.Configuration.Kubernetes;

internal sealed class KubernetesConfigSourceSettings
{
    public string Name { get; }
    public string Namespace { get; }
    public ReloadSettings ReloadSettings { get; }
    public ILoggerFactory? LoggerFactory { get; set; }

    public KubernetesConfigSourceSettings(string? @namespace, string name, ReloadSettings reloadSettings, ILoggerFactory? loggerFactory = null)
    {
        ArgumentGuard.NotNull(name);
        ArgumentGuard.NotNull(reloadSettings);

        Namespace = @namespace ?? "default";
        Name = name;
        ReloadSettings = reloadSettings;
        LoggerFactory = loggerFactory;
    }
}

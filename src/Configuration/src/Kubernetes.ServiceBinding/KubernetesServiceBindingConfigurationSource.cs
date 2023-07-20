// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Steeltoe.Common;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding;

internal sealed class KubernetesServiceBindingConfigurationSource : PostProcessorConfigurationSource, IConfigurationSource
{
    public IFileProvider? FileProvider { get; }

    public bool ReloadOnChange { get; set; }

    public int ReloadDelay { get; set; } = 250;

    public bool Optional { get; set; } = true;

    public KubernetesServiceBindingConfigurationSource(IServiceBindingsReader reader)
    {
        ArgumentGuard.NotNull(reader);

        FileProvider = reader.GetRootDirectory();
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        CaptureConfigurationBuilder(builder);
        return new KubernetesServiceBindingConfigurationProvider(this);
    }
}

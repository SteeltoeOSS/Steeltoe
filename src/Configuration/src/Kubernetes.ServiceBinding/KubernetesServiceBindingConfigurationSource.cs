// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding;

internal sealed class KubernetesServiceBindingConfigurationSource : PostProcessorConfigurationSource, IConfigurationSource
{
    internal const string ServiceBindingRootDirEnvVariable = "SERVICE_BINDING_ROOT";

    public IFileProvider FileProvider { get; set; }

    public string ServiceBindingRoot { get; set; }

    public bool ReloadOnChange { get; set; }

    public int ReloadDelay { get; set; } = 250;

    public bool Optional { get; set; } = true;

    public KubernetesServiceBindingConfigurationSource()
        : this(Environment.GetEnvironmentVariable(ServiceBindingRootDirEnvVariable))
    {
    }

    public KubernetesServiceBindingConfigurationSource(string serviceBindingRootDirectory)
    {
        ServiceBindingRoot = serviceBindingRootDirectory != null ? Path.GetFullPath(serviceBindingRootDirectory) : null;

        if (Directory.Exists(ServiceBindingRoot))
        {
            var fileProvider = new PhysicalFileProvider(ServiceBindingRoot, ExclusionFilters.Sensitive);

            if (ReloadOnChange)
            {
                fileProvider.UsePollingFileWatcher = true;
                fileProvider.UseActivePolling = true;
            }

            FileProvider = fileProvider;
        }
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        ParentConfiguration ??= GetParentConfiguration(builder);

        return new KubernetesServiceBindingConfigurationProvider(this);
    }
}

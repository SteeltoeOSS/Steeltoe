// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding;
internal class ServiceBindingConfigurationSource : PostProcessorConfigurationSource, IConfigurationSource
{
    internal const string ServiceBindingRootDirEnvVariable = "SERVICE_BINDING_ROOT";

    public ServiceBindingConfigurationSource()
        : this(Environment.GetEnvironmentVariable(ServiceBindingRootDirEnvVariable))
    {
    }

    public ServiceBindingConfigurationSource(string? serviceBindingRootDirectory)
    {
        ServiceBindingRoot = serviceBindingRootDirectory != null ? Path.GetFullPath(serviceBindingRootDirectory) : serviceBindingRootDirectory;
        if (ServiceBindingRoot != null)
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

    public IFileProvider? FileProvider { get; set; }

    public string? ServiceBindingRoot { get; set; }

    public bool ReloadOnChange { get; set; }

    public int ReloadDelay { get; set; } = 250;

    public bool Optional { get; set; }

    public Predicate<string> IgnoreKeyPredicate { get; set; } = (p) => false;

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new ServiceBindingConfigurationProvider(this);
    }
}

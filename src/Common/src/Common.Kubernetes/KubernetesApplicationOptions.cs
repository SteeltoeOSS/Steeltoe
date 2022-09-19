// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Common.Kubernetes;

public sealed class KubernetesApplicationOptions : ApplicationInstanceInfo
{
    private const string PlatformConfigurationRoot = "spring:cloud:kubernetes";

    protected override string PlatformRoot => PlatformConfigurationRoot;

    public bool Enabled { get; set; } = true;

    public override string ApplicationName => Name;

    public string NameSpace { get; set; } = "default";

    /// <summary>
    /// Gets or sets properties for if/how reloading configuration data.
    /// </summary>
    public ReloadSettings Reload { get; set; }

    /// <summary>
    /// Gets or sets general Kubernetes and ConfigMap configuration properties.
    /// </summary>
    public KubernetesConfiguration Config { get; set; }

    /// <summary>
    /// Gets or sets configuration properties of Secrets.
    /// </summary>
    public WatchableResource Secrets { get; set; }

    /// <summary>
    /// Gets or sets the character used to separate the app and environment names when used for retrieving ConfigMaps or Secrets.
    /// </summary>
    public string NameEnvironmentSeparator { get; set; } = ".";

    // This constructor is for use with IOptions.
    public KubernetesApplicationOptions()
    {
    }

    public KubernetesApplicationOptions(IConfiguration configuration)
        : base(configuration.GetSection(PlatformConfigurationRoot))
    {
        // override base class's use of configuration sub-section so that we can find spring:application:name
        Configuration = configuration;

        Name ??= GetApplicationNameInContext(SteeltoeComponent.Kubernetes);
        Config ??= new KubernetesConfiguration();
        Secrets ??= new WatchableResource();
        Reload ??= new ReloadSettings();
    }
}

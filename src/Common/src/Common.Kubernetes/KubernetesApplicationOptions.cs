// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Common.Kubernetes;

public class KubernetesApplicationOptions : ApplicationInstanceInfo
{
    public static string PlatformConfigurationRoot => "spring:cloud:kubernetes";

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
    /// Gets or sets configuration properties of secrets.
    /// </summary>
    public WatchableResource Secrets { get; set; }

    /// <summary>
    /// Gets or sets the character used to separate the app and environment names when used for retrieving configuration maps or secrets.
    /// </summary>
    public string NameEnvironmentSeparator { get; set; } = ".";

    public KubernetesApplicationOptions()
    {
    }

    public KubernetesApplicationOptions(IConfiguration configuration)
        : base(configuration.GetSection(PlatformConfigurationRoot))
    {
        // override base class's use of configuration sub-section so that we can find spring:application:name
        this.configuration = configuration;

        Name ??= ApplicationNameInContext(SteeltoeComponent.Kubernetes);
        Config ??= new KubernetesConfiguration();
        Secrets ??= new WatchableResource();
        Reload ??= new ReloadSettings();
    }
}

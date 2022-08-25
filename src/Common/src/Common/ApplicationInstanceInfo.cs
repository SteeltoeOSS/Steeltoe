// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Configuration;
using Steeltoe.Common.Options;

namespace Steeltoe.Common;

public class ApplicationInstanceInfo : AbstractOptions, IApplicationInstanceInfo
{
    public const string ApplicationRoot = "application";
    public const string SpringApplicationRoot = "spring:application";
    public const string ServicesRoot = "services";
    public const string EurekaRoot = "eureka";
    public const string ConfigServerRoot = "spring:cloud:config";
    public const string ConsulRoot = "consul";
    public const string KubernetesRoot = "spring:cloud:kubernetes";
    public const string ManagementRoot = "management";

    protected IConfiguration configuration;

    protected virtual string PlatformRoot => string.Empty;

    public string DefaultAppName => Assembly.GetEntryAssembly().GetName().Name;

    public string AppNameKey => $"{SpringApplicationRoot}:name";

    public string AppInstanceIdKey => $"{SpringApplicationRoot}:instance_id";

    public string ConfigServerNameKey => $"{ConfigServerRoot}:name";

    public string ConsulInstanceNameKey => $"{ConsulRoot}:serviceName";

    public string EurekaInstanceNameKey => $"{EurekaRoot}:instance:appName";

    public string KubernetesNameKey => $"{KubernetesRoot}:name";

    public string ManagementNameKey => $"{ManagementRoot}:name";

    public string PlatformNameKey => BuildConfigString(PlatformRoot, $"{ApplicationRoot}:name");

    // ReSharper disable once InconsistentNaming
    public string Instance_Id { get; set; }

    public virtual string InstanceId
    {
        get => Instance_Id;
        set => Instance_Id = value;
    }

    // ReSharper disable once InconsistentNaming
    public string Application_Id { get; set; }

    public virtual string ApplicationId
    {
        get => Application_Id;
        set => Application_Id = value;
    }

    public virtual string Name { get; set; }

    public virtual string ApplicationName => Name ?? configuration?.GetValue(AppNameKey, DefaultAppName);

    public virtual string ApplicationVersion { get; set; }

    public virtual string EnvironmentName { get; set; } = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
        ? "Production"
        : Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    public virtual int InstanceIndex { get; set; } = -1;

    public int Port { get; set; } = -1;

    public virtual IEnumerable<string> Uris { get; set; }

    public string Version { get; set; }

    public virtual int DiskLimit { get; set; } = -1;

    public virtual int MemoryLimit { get; set; } = -1;

    public virtual int FileDescriptorLimit { get; set; } = -1;

    public virtual string InstanceIp { get; set; }

    public virtual string InternalIp { get; set; }

    public ApplicationInstanceInfo()
    {
        SecondChanceSetIdProperties();
    }

    public ApplicationInstanceInfo(IConfiguration configuration)
        : base(configuration)
    {
        this.configuration = configuration;
        SecondChanceSetIdProperties(this.configuration);
    }

    public ApplicationInstanceInfo(IConfiguration configuration, bool noPrefix)
        : base(configuration, ApplicationRoot)
    {
        this.configuration = configuration;
        SecondChanceSetIdProperties(this.configuration);
    }

    public ApplicationInstanceInfo(IConfiguration configuration, string configPrefix)
        : base(configuration, BuildConfigString(configPrefix, ApplicationRoot))
    {
        this.configuration = configuration;
        SecondChanceSetIdProperties(this.configuration);
    }

    protected void SecondChanceSetIdProperties(IConfiguration configuration = null)
    {
        if (configuration != null)
        {
            Instance_Id ??= configuration.GetValue<string>(AppInstanceIdKey);
            Application_Id ??= configuration.GetValue<string>($"{SpringApplicationRoot}:id");
        }
    }

    private static string BuildConfigString(string prefix, string key)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return key;
        }

        return $"{prefix}:{key}";
    }

    public string GetApplicationNameInContext(SteeltoeComponent steeltoeComponent, string additionalSearchPath = null)
    {
        return steeltoeComponent switch
        {
            SteeltoeComponent.Configuration => ConfigurationValuesHelper.GetPreferredSetting(configuration, DefaultAppName, additionalSearchPath,
                ConfigServerNameKey, PlatformNameKey, AppNameKey),
            SteeltoeComponent.Discovery => ConfigurationValuesHelper.GetPreferredSetting(configuration, DefaultAppName, additionalSearchPath,
                EurekaInstanceNameKey, ConsulInstanceNameKey, PlatformNameKey, AppNameKey),
            SteeltoeComponent.Kubernetes => ConfigurationValuesHelper.GetPreferredSetting(configuration, DefaultAppName, additionalSearchPath,
                KubernetesNameKey, PlatformNameKey, AppNameKey),
            SteeltoeComponent.Management => ConfigurationValuesHelper.GetPreferredSetting(configuration, DefaultAppName, additionalSearchPath,
                ManagementNameKey, PlatformNameKey, AppNameKey),
            _ => ConfigurationValuesHelper.GetPreferredSetting(configuration, DefaultAppName, additionalSearchPath, PlatformNameKey, AppNameKey)
        };
    }
}

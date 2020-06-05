// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Configuration;
using Steeltoe.Common.Options;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Common
{
    public class ApplicationInstanceInfo : AbstractOptions, IApplicationInstanceInfo
    {
        public static readonly string ApplicationRoot = "application";
        public readonly string SpringApplicationRoot = "spring:application";
        public readonly string ServicesRoot = "services";
        public readonly string EurekaRoot = "eureka";
        public readonly string ConfigServerRoot = "spring:cloud:config";
        public readonly string ConsulRoot = "consul";
        public readonly string KubernetesRoot = "spring:cloud:kubernetes";
        public readonly string ManagementRoot = "management";

        public string DefaultAppName => Assembly.GetEntryAssembly().GetName().Name;

        public string AppNameKey => SpringApplicationRoot + ":name";

        public string AppInstanceIdKey => SpringApplicationRoot + ":instance_id";

        public string ConfigServerNameKey => ConfigServerRoot + ":name";

        public string ConsulInstanceNameKey => ConsulRoot + ":serviceName";

        public string EurekaInstanceNameKey => EurekaRoot + ":instance:appName";

        public string KubernetesNameKey => KubernetesRoot + ":config:name";

        public string ManagementNameKey => ManagementRoot + ":name";

        public string PlatformNameKey => BuildConfigString(PlatformRoot, ApplicationRoot + ":name");

        protected virtual string PlatformRoot => string.Empty;

        protected void SecondChanceSetIdProperties(IConfiguration config = null)
        {
            if (config != null)
            {
                Instance_Id ??= config.GetValue<string>(AppInstanceIdKey);
                Application_Id ??= config.GetValue<string>(SpringApplicationRoot + ":id");
            }
        }

        private static string BuildConfigString(string prefix, string key)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return key;
            }
            else
            {
                return prefix + ":" + key;
            }
        }

        protected IConfiguration configuration;

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

        public ApplicationInstanceInfo(IConfiguration configuration, string configPrefix)
            : base(configuration, BuildConfigString(configPrefix, ApplicationRoot))
        {
            this.configuration = configuration;
            SecondChanceSetIdProperties(this.configuration);
        }

        public string Instance_Id { get; set; }

        public virtual string InstanceId
        {
            get { return Instance_Id; }
            set { Instance_Id = value; }
        }

        public string Application_Id { get; set; }

        public virtual string ApplicationId
        {
            get { return Application_Id; }
            set { Application_Id = value; }
        }

        public virtual string ApplicationName => configuration?.GetValue(AppNameKey, DefaultAppName);

        public string ApplicationNameInContext(SteeltoeComponent steeltoeComponent, string additionalSearchPath = null)
        {
            return steeltoeComponent switch
            {
                SteeltoeComponent.Configuration => ConfigurationValuesHelper.GetPreferredSetting(configuration, DefaultAppName, additionalSearchPath, ConfigServerNameKey, PlatformNameKey, AppNameKey),
                SteeltoeComponent.Discovery => ConfigurationValuesHelper.GetPreferredSetting(configuration, DefaultAppName, additionalSearchPath, EurekaInstanceNameKey, ConsulInstanceNameKey, PlatformNameKey, AppNameKey),
                SteeltoeComponent.Kubernetes => ConfigurationValuesHelper.GetPreferredSetting(configuration, DefaultAppName, additionalSearchPath, KubernetesNameKey, PlatformNameKey, AppNameKey),
                SteeltoeComponent.Management => ConfigurationValuesHelper.GetPreferredSetting(configuration, DefaultAppName, additionalSearchPath, ManagementNameKey, PlatformNameKey, AppNameKey),
                _ => ConfigurationValuesHelper.GetPreferredSetting(configuration, DefaultAppName, additionalSearchPath, PlatformNameKey, AppNameKey)
            };
        }

        public virtual string ApplicationVersion { get; set; }

        public virtual string EnvironmentName { get; set; } = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")) ? "Production" : Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        public virtual int InstanceIndex { get; set; } = -1;

        public int Port { get; set; } = -1;

        public virtual IEnumerable<string> Uris { get; set; }

        public string Version { get; set; }

        public virtual int DiskLimit { get; set; } = -1;

        public virtual int MemoryLimit { get; set; } = -1;

        public virtual int FileDescriptorLimit { get; set; } = -1;

        public virtual string InstanceIP { get; set; }

        public virtual string InternalIP { get; set; }
    }
}

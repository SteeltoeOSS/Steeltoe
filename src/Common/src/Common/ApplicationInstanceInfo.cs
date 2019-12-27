// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Configuration;
using Steeltoe.Common.Options;
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
        public readonly string ManagementRoot = "management";

        public string DefaultAppName => Assembly.GetEntryAssembly().GetName().Name;

        public string AppNameKey => SpringApplicationRoot + ":name";

        public string AppInstanceIdKey => SpringApplicationRoot + ":instance_id";

        public string ConfigServerNameKey => ConfigServerRoot + ":name";

        public string ConsulInstanceNameKey => ConsulRoot + ":serviceName";

        public string EurekaInstanceNameKey => EurekaRoot + ":instance:appName";

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

        private readonly IConfiguration configuration;

        public ApplicationInstanceInfo()
        {
            SecondChanceSetIdProperties();
        }

        public ApplicationInstanceInfo(IConfiguration configuration, string configPrefix = "")
            : base(configuration, BuildConfigString(configPrefix, "application"))
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
                SteeltoeComponent.Management => ConfigurationValuesHelper.GetPreferredSetting(configuration, DefaultAppName, additionalSearchPath, ManagementNameKey, PlatformNameKey, AppNameKey),
                _ => ConfigurationValuesHelper.GetPreferredSetting(configuration, DefaultAppName, additionalSearchPath, PlatformNameKey, AppNameKey)
            };
        }

        public virtual string ApplicationVersion { get; set; }

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

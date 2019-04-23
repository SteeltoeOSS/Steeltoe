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
using Steeltoe.Common.Options;

namespace Steeltoe.Extensions.Configuration.CloudFoundry
{
    public class CloudFoundryApplicationOptions : AbstractOptions
    {
        public const string CONFIGURATION_PREFIX = "vcap:application";

        public CloudFoundryApplicationOptions()
        {
        }

        public CloudFoundryApplicationOptions(IConfigurationRoot root)
            : base(root, CONFIGURATION_PREFIX)
        {
        }

        public CloudFoundryApplicationOptions(IConfiguration config)
            : base(config)
        {
        }

        public string CF_Api { get; set; }

        public string Application_Id { get; set; }

        public string Application_Name { get; set; }

        public string[] Application_Uris { get; set; }

        public string Application_Version { get; set; }

        public string Instance_Id { get; set; }

        public int Instance_Index { get; set; } = -1;

        public Limits Limits { get; set; }

        public string Name { get; set; }

        public int Port { get; set; } = -1;

        public string Space_Id { get; set; }

        public string Space_Name { get; set; }

        public string Start { get; set; }

        public string[] Uris { get; set; }

        public string Version { get; set; }

        public string Instance_IP { get; set; }

        public string Internal_IP { get; set; }

        public string ApplicationId => Application_Id;

        public string ApplicationName => Application_Name;

        public string[] ApplicationUris => Application_Uris;

        public string ApplicationVersion => Application_Version;

        public string InstanceId => Instance_Id;

        public int InstanceIndex => Instance_Index;

        public string SpaceId => Space_Id;

        public string SpaceName => Space_Name;

        public string InstanceIP => Instance_IP;

        public string InternalIP => Internal_IP;

        public int DiskLimit => Limits == null ? -1 : Limits.Disk;

        public int MemoryLimit => Limits == null ? -1 : Limits.Mem;

        public int FileDescriptorLimit => Limits == null ? -1 : Limits.Fds;
    }
}

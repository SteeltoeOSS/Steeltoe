// Copyright 2015 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;

namespace Steeltoe.CloudFoundry.Connector.App
{
    public class ApplicationInstanceInfo : IApplicationInstanceInfo
    {
        internal ApplicationInstanceInfo()
        {
        }

        internal ApplicationInstanceInfo(CloudFoundryApplicationOptions opts)
        {
            InstanceId = GetInstanceId(opts);
            ApplicationId = opts.ApplicationId;
            ApplicationName = opts.ApplicationName;
            ApplicationUris = opts.ApplicationUris;
            ApplicationVersion = opts.ApplicationVersion;
            InstanceIndex = opts.InstanceIndex;
            Port = opts.Port;
            SpaceId = opts.SpaceId;
            SpaceName = opts.SpaceName;
            Uris = opts.Uris;
            Version = opts.Version;
            DiskLimit = opts.DiskLimit;
            MemoryLimit = opts.MemoryLimit;
            FileDescriptorLimit = opts.FileDescriptorLimit;
            InstanceIP = opts.InstanceIP;
            InternalIP = opts.InternalIP;
        }

        public string InstanceId { get; internal protected set; }

        public string ApplicationId { get; internal protected set; }

        public string ApplicationName { get; internal protected set; }

        public string[] ApplicationUris { get; internal protected set; }

        public string ApplicationVersion { get; internal protected set; }

        public int InstanceIndex { get; internal protected set; }

        public int Port { get; internal protected set; }

        public string SpaceId { get; internal protected set; }

        public string SpaceName { get; internal protected set; }

        public string[] Uris { get; internal protected set; }

        public string Version { get; internal protected set; }

        public int DiskLimit { get; internal protected set; }

        public int MemoryLimit { get; internal protected set; }

        public int FileDescriptorLimit { get; internal protected set; }

        public string InstanceIP { get; internal protected set; }

        public string InternalIP { get; internal protected set; }

        private string GetInstanceId(CloudFoundryApplicationOptions opts)
        {
            if (!string.IsNullOrEmpty(opts.InstanceId))
            {
                return opts.InstanceId;
            }

            return Environment.GetEnvironmentVariable("CF_INSTANCE_GUID");
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Configuration.CloudFoundry
{
    public class CloudFoundryApplicationOptions : ApplicationInstanceInfo
    {
        public static string PlatformConfigRoot => "vcap";

        protected override string PlatformRoot => PlatformConfigRoot;

        public CloudFoundryApplicationOptions()
        {
            SecondChanceSetIdProperties();
        }

        public CloudFoundryApplicationOptions(IConfiguration config)
            : base(config, PlatformConfigRoot)
        {
            SetIdPropertiesFromVCAP(config);
        }

        private void SetIdPropertiesFromVCAP(IConfiguration config = null)
        {
            if (config != null)
            {
                var vcapInstanceId = config.GetValue<string>(PlatformConfigRoot + ":application:instance_id");
                if (!string.IsNullOrEmpty(vcapInstanceId))
                {
                    Instance_Id = vcapInstanceId;
                }

                var vcapAppId = config.GetValue<string>(PlatformConfigRoot + ":application:id");
                if (!string.IsNullOrEmpty(vcapAppId))
                {
                    Application_Id = vcapAppId;
                }
            }
        }

        public string CF_Api { get; set; }

        public override string ApplicationName => Name;

        public string Start { get; set; }

        public IEnumerable<string> Application_Uris { get; set; }

        public IEnumerable<string> ApplicationUris => Application_Uris;

        public string Application_Version { get; set; }

        public override string ApplicationVersion => Application_Version;

        public int Instance_Index { get; set; } = -1;

        public override int InstanceIndex => Instance_Index;

        public string Space_Id { get; set; }

        public string SpaceId => Space_Id;

        public string Space_Name { get; set; }

        public string SpaceName => Space_Name;

        public string Instance_IP { get; set; }

        public override string InstanceIP => Instance_IP;

        public string Internal_IP { get; set; }

        public override string InternalIP => Internal_IP;

        public Limits Limits { get; set; }

        public override int DiskLimit => Limits == null ? -1 : Limits.Disk;

        public override int MemoryLimit => Limits == null ? -1 : Limits.Mem;

        public override int FileDescriptorLimit => Limits == null ? -1 : Limits.Fds;
    }
}

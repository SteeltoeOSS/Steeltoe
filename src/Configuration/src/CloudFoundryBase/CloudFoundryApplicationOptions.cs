// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Extensions.Configuration.CloudFoundry;

public class CloudFoundryApplicationOptions : ApplicationInstanceInfo
{
    public static string PlatformConfigRoot => "vcap";

    protected override string PlatformRoot => PlatformConfigRoot;

    // ReSharper disable once InconsistentNaming
    public string CF_Api { get; set; }

    public override string ApplicationName => Name;

    public string Start { get; set; }

    // ReSharper disable once InconsistentNaming
    public IEnumerable<string> Application_Uris { get; set; }

    public IEnumerable<string> ApplicationUris => Application_Uris;

    // ReSharper disable once InconsistentNaming
    public string Application_Version { get; set; }

    public override string ApplicationVersion => Application_Version;

    // ReSharper disable once InconsistentNaming
    public int Instance_Index { get; set; } = -1;

    public override int InstanceIndex => Instance_Index;

    // ReSharper disable once InconsistentNaming
    public string Space_Id { get; set; }

    public string SpaceId => Space_Id;

    // ReSharper disable once InconsistentNaming
    public string Space_Name { get; set; }

    public string SpaceName => Space_Name;

    // ReSharper disable once InconsistentNaming
    public string Instance_IP { get; set; }

    public override string InstanceIp => Instance_IP;

    // ReSharper disable once InconsistentNaming
    public string Internal_IP { get; set; }

    public override string InternalIp => Internal_IP;

    public Limits Limits { get; set; }

    public override int DiskLimit => Limits?.Disk ?? -1;

    public override int MemoryLimit => Limits?.Mem ?? -1;

    public override int FileDescriptorLimit => Limits?.Fds ?? -1;

    public CloudFoundryApplicationOptions()
    {
        SecondChanceSetIdProperties();
    }

    public CloudFoundryApplicationOptions(IConfiguration config)
        : base(config, PlatformConfigRoot)
    {
        SetIdPropertiesFromVcap(config);
    }

    private void SetIdPropertiesFromVcap(IConfiguration config = null)
    {
        if (config != null)
        {
            string vcapInstanceId = config.GetValue<string>($"{PlatformConfigRoot}:application:instance_id");

            if (!string.IsNullOrEmpty(vcapInstanceId))
            {
                Instance_Id = vcapInstanceId;
            }

            string vcapAppId = config.GetValue<string>($"{PlatformConfigRoot}:application:id");

            if (!string.IsNullOrEmpty(vcapAppId))
            {
                Application_Id = vcapAppId;
            }
        }
    }
}

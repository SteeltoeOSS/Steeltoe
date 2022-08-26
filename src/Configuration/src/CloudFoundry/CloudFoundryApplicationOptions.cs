// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Extensions.Configuration.CloudFoundry;

public sealed class CloudFoundryApplicationOptions : ApplicationInstanceInfo
{
    internal const string PlatformConfigurationRoot = "vcap";

    protected override string PlatformRoot => PlatformConfigurationRoot;

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

    public override string InstanceIP => Instance_IP;

    // ReSharper disable once InconsistentNaming
    public string Internal_IP { get; set; }

    public override string InternalIP => Internal_IP;

    public Limits Limits { get; set; }

    public override int DiskLimit => Limits?.Disk ?? -1;

    public override int MemoryLimit => Limits?.Mem ?? -1;

    public override int FileDescriptorLimit => Limits?.Fds ?? -1;

    // This constructor is for use with IOptions.
    public CloudFoundryApplicationOptions()
    {
        SecondChanceSetIdProperties();
    }

    public CloudFoundryApplicationOptions(IConfiguration configuration)
        : base(configuration, PlatformConfigurationRoot)
    {
        SetIdPropertiesFromVcap(configuration);
    }

    private void SetIdPropertiesFromVcap(IConfiguration configuration = null)
    {
        if (configuration != null)
        {
            string vcapInstanceId = configuration.GetValue<string>($"{PlatformConfigurationRoot}:application:instance_id");

            if (!string.IsNullOrEmpty(vcapInstanceId))
            {
                Instance_Id = vcapInstanceId;
            }

            string vcapAppId = configuration.GetValue<string>($"{PlatformConfigurationRoot}:application:id");

            if (!string.IsNullOrEmpty(vcapAppId))
            {
                Application_Id = vcapAppId;
            }
        }
    }
}

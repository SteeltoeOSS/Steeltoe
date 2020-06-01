// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Extensions.Configuration.CloudFoundry;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.App.Test
{
    public class ApplicationInstanceInfoTest
    {
        public static CloudFoundryApplicationOptions MakeCloudFoundryApplicationOptions()
        {
            CloudFoundryApplicationOptions opts = new CloudFoundryApplicationOptions()
            {
                Application_Id = "Application_Id",
                Application_Name = "Application_Name",
                Application_Uris = new string[] { "Application_Uris" },
                Application_Version = "Application_Version",
                Instance_Id = "Instance_Id",
                Limits = new Limits()
                {
                    Disk = 1,
                    Fds = 1,
                    Mem = 1
                },
                Name = "Name",
                Space_Id = "Space_Id",
                Space_Name = "Space_Name",
                Start = "Start",
                Uris = new string[] { "uris" },
                Version = "Version",
            };
            return opts;
        }

        [Fact]
        public void Constructor_BuildsExpectedFromOpts()
        {
            CloudFoundryApplicationOptions opts = MakeCloudFoundryApplicationOptions();
            ApplicationInstanceInfo info = new ApplicationInstanceInfo(opts);
            Assert.Equal(opts.ApplicationId, info.ApplicationId);
            Assert.Equal(opts.ApplicationName, info.ApplicationName);
            Assert.Equal(opts.ApplicationUris[0], info.ApplicationUris[0]);
            Assert.Equal(opts.ApplicationVersion, info.ApplicationVersion);
            Assert.Equal(opts.DiskLimit, info.DiskLimit);
            Assert.Equal(opts.FileDescriptorLimit, info.FileDescriptorLimit);
            Assert.Equal(opts.InstanceId, info.InstanceId);
            Assert.Equal(opts.InstanceIndex, info.InstanceIndex);
            Assert.Equal(opts.MemoryLimit, info.MemoryLimit);
            Assert.Equal(opts.Port, info.Port);
            Assert.Equal(opts.SpaceId, info.SpaceId);
            Assert.Equal(opts.SpaceName, info.SpaceName);
            Assert.Equal(opts.Uris[0], info.Uris[0]);
            Assert.Equal(opts.Version, info.Version);
        }
    }
}

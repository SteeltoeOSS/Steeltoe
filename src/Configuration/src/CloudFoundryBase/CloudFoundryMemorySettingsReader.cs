// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Extensions.Configuration.CloudFoundry
{
    public class CloudFoundryMemorySettingsReader : ICloudFoundrySettingsReader
    {
        public string ApplicationJson { get; set; }

        public string InstanceId { get; set; }

        public string InstanceIndex { get; set; }

        public string InstanceInternalIp { get; set; }

        public string InstanceIp { get; set; }

        public string InstancePort { get; set; }

        public string ServicesJson { get; set; }
    }
}
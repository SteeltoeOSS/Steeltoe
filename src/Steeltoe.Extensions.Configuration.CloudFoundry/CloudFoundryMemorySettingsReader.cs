using System;
using System.Linq;

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

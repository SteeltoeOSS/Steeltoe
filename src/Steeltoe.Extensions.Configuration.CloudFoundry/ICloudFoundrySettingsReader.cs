using System;
using System.Linq;

namespace Steeltoe.Extensions.Configuration.CloudFoundry
{
    public interface ICloudFoundrySettingsReader
    {
        string ApplicationJson { get; }

        string InstanceId { get; }

        string InstanceIndex { get; }

        string InstanceInternalIp { get; }

        string InstanceIp { get; }

        string InstancePort { get; }

        string ServicesJson { get; }
    }
}

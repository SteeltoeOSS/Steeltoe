// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CloudFoundry.Connector.App
{
    public interface IApplicationInstanceInfo
    {
        string InstanceId { get;  }

        string ApplicationId { get;  }

        string ApplicationName { get; }

        string[] ApplicationUris { get;  }

        string ApplicationVersion { get; }

        int InstanceIndex { get;  }

        int Port { get;  }

        string SpaceId { get; }

        string SpaceName { get; }

        string[] Uris { get;  }

        string Version { get; }

        int DiskLimit { get;  }

        int MemoryLimit { get;  }

        int FileDescriptorLimit { get;  }

        string InstanceIP { get; }

        string InternalIP { get; }
    }
}

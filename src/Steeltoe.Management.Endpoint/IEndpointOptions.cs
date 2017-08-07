using Steeltoe.Management.Endpoint.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Endpoint
{
    public interface IEndpointOptions
    {
        bool Enabled { get; }
        bool Sensitive { get;  }
        IManagementOptions Global { get; }
        string Id { get;  }
        string Path { get; }
        Permissions RequiredPermissions { get; }
        bool IsAccessAllowed(Permissions permissions);
    }
}

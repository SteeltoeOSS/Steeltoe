using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Endpoint
{
    public interface IManagementOptions
    {
        bool Enabled { get; }
        bool Sensitive { get; }
        string Path { get; }
        List<IEndpointOptions> EndpointOptions { get;  }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.CloudFoundry
{
    public interface ICloudFoundryOptions : IEndpointOptions
    {
        bool ValidateCertificates { get; }
        string ApplicationId { get; }
        string CloudFoundryApi { get; }
    }
}

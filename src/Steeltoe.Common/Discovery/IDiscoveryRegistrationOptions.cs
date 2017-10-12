using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Discovery.Client
{
    public interface IDiscoveryRegistrationOptions
    {
        string RegistrationMethod { get; set; }  // e.g. 'route', 'direct', 'hostname'
    }
}

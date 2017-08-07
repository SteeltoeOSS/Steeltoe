using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Health
{
    public enum HealthStatus
    {
        UNKNOWN,
        UP,
        OUT_OF_SERVICE,
        DOWN,
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Info.Test
{
    public class BaseTest : IDisposable
    {
        public BaseTest()
        {
            ManagementOptions._instance = null;
        }

        public void Dispose()
        {
            ManagementOptions._instance = null;
        }
    }
}

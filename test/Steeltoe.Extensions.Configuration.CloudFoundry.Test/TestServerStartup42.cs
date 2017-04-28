using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Steeltoe.Extensions.Configuration.CloudFoundry.Test
{
    public class TestServerStartup42 : TestServerStartup
    {
        public TestServerStartup42()
        {
            this.ExpectedAddresses.Add("http://*:42");
        }
    }
}

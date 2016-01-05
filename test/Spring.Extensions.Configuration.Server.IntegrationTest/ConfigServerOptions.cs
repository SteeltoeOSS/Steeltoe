using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spring.Extensions.Configuration.Server.IntegrationTest
{
    public class ConfigServerOptions
    {
        public string Bar { get; set; }
        public string Foo { get; set; }
        public Info Info { get; set; }

    }

    public class Info
    {
        public string Description { get; set; }
        public string Url { get; set;  }
    }

}

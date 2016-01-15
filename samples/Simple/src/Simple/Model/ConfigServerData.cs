using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Simple.Model
{
    /// <summary>
    /// An object used with the DI Options mechanism for exposing the data retrieved 
    /// from the Spring Cloud Config Server
    /// </summary>
    public class ConfigServerData
    {
        public string Bar { get; set; }
        public string Foo { get; set; }
        public Info Info { get; set; }

    }

    public class Info
    {
        public string Description { get; set; }
        public string Url { get; set; }
    }
}

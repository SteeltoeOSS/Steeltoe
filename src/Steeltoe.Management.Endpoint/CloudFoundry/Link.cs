using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.CloudFoundry
{
    public class Link
    {
        public Link() { }
        public Link(string href)
        {
            this.href = href;
        }

        public string href;
    }
}

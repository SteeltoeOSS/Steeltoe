using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.CloudFoundry
{
    public class CloudFoundryEndpoint : AbstractEndpoint<Links, string>
    {
        protected new ICloudFoundryOptions Options
        {
            get
            {
                return options as ICloudFoundryOptions;
            }
        }

        public CloudFoundryEndpoint(ICloudFoundryOptions options) : base(options)
        {
        }

        public override Links Invoke(string baseUrl)
        {
            var endpointOptions = Options.Global.EndpointOptions;
            var links = new Links();

            //links._links.Add("self", new Link(baseUrl));
            //links._links.Add("jolokia", new Link(baseUrl + "/" + "jolokia"));
            //links._links.Add("health", new Link(baseUrl + "/" + "health"));
            //links._links.Add("loggers", new Link(baseUrl + "/" + "loggers"));
            //links._links.Add("heapdump", new Link(baseUrl + "/" + "heapdump"));
            //links._links.Add("beans", new Link(baseUrl + "/" + "beans"));
            //links._links.Add("auditevents", new Link(baseUrl + "/" + "auditevents"));
            //links._links.Add("dump", new Link(baseUrl + "/" + "dump"));
            //links._links.Add("mappings", new Link(baseUrl + "/" + "mappings"));
            //links._links.Add("autoconfig", new Link(baseUrl + "/" + "autoconfig"));
            //links._links.Add("configprops", new Link(baseUrl + "/" + "configprops"));
            //links._links.Add("env", new Link(baseUrl + "/" + "env"));
            //links._links.Add("trace", new Link(baseUrl + "/" + "trace"));
            //links._links.Add("info", new Link(baseUrl + "/" + "info"));
            //links._links.Add("metrics", new Link(baseUrl + "/" + "metrics"));

            foreach (var opt in endpointOptions)
            {
                if (!opt.Enabled)
                    continue;

                if (opt == Options)
                {
                    links._links.Add("self", new Link(baseUrl));
                }
                else
                {
                    links._links.Add(opt.Id, new Link(baseUrl + "/" + opt.Id));
                }
            }

            return links;
            
        }
    
    }
}

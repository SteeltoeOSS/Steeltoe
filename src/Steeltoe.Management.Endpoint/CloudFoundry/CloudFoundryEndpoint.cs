//
// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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

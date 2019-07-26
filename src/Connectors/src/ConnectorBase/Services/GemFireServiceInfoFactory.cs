// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Extensions.Configuration.CloudFoundry;
using System.Linq;

namespace Steeltoe.CloudFoundry.Connector.Services
{
    public class GemFireServiceInfoFactory : ServiceInfoFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GemFireServiceInfoFactory"/> class.
        /// </summary>
        /// <remarks>
        /// Scheme will never really be valid unless the service binding begins to provide a uri.
        /// The success of this provider will depend on the tags
        /// </remarks>
        public GemFireServiceInfoFactory()
            : base(new Tags(new string[] { "gemfire", "cloudcache" }), "gemfire")
        {
        }

        public override IServiceInfo Create(Service binding)
        {
            var sInfo = new GemFireServiceInfo(binding.Name);

            // urls
            foreach (var url in binding.Credentials["urls"])
            {
                sInfo.Urls.Add(url.Key, url.Value.Value);
            }

            // locators
            sInfo.Locators.AddRange(binding.Credentials["locators"].Values.Select(l => l.Value));

            // users
            foreach (var user in binding.Credentials["users"])
            {
                sInfo.Users.Add(new GemFireUser
                    {
                        Username = user.Value["username"].Value,
                        Password = user.Value["password"].Value,
                        Roles = user.Value["roles"].Values.Select(r => r.Value)
                    });
            }

            return sInfo;
        }
    }
}

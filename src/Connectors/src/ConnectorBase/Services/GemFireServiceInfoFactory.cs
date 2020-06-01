// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

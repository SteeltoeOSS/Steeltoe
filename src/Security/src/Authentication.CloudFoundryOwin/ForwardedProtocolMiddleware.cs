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

using Microsoft.Owin;
using System.Threading.Tasks;

namespace Steeltoe.Security.Authentication.CloudFoundry.Owin
{
    public class ForwardedProtocolMiddleware : OwinMiddleware
    {
        public ForwardedProtocolMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        public override async Task Invoke(IOwinContext context)
        {
            if (context.Request.Headers["X-Forwarded-Proto"] == "https" && context.Request.Scheme != "https")
            {
                context.Request.Scheme = "https";
            }

            await Next.Invoke(context).ConfigureAwait(false);
        }
    }
}

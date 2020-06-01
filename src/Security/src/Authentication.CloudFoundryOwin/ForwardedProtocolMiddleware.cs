// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

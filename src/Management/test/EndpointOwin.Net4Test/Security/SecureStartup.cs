// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Owin;

namespace Steeltoe.Management.EndpointOwin.Security.Test
{
    public class SecureStartup : Startup
    {
        public override void Configuration(IAppBuilder app)
        {
            app.Use<AuthenticationTestMiddleware>();
            base.Configuration(app);
        }
    }
}

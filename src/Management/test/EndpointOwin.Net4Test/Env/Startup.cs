// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Owin;
using Steeltoe.Management.EndpointOwin.Test;

namespace Steeltoe.Management.EndpointOwin.Env.Test
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(OwinTestHelpers.Appsettings);
            var config = builder.Build();

            app.UseEnvActuator(config);
        }
    }
}

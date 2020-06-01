// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Owin;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.EndpointOwin.Info;
using Steeltoe.Management.EndpointOwin.Test;
using System.Collections.Generic;

namespace Steeltoe.Management.EndpointOwin.Hypermedia.Test
{
    public class StartupWithSecurity
    {
        public void Configuration(IAppBuilder app)
        {
            var builder = new ConfigurationBuilder();

            var appSettings = new Dictionary<string, string>(OwinTestHelpers.Appsettings)
            {
                ["management:endpoints:path"] = "/",
                ["info:application:name"] = "foobar",
                ["info:application:version"] = "1.0.0",
                ["info:application:date"] = "5/1/2008",
                ["info:application:time"] = "8:30:52 AM",
                ["info:NET:type"] = "Core",
                ["info:NET:version"] = "2.0.0",
                ["info:NET:ASPNET:type"] = "Core",
                ["info:NET:ASPNET:version"] = "2.0.0"
            };

            builder.AddInMemoryCollection(appSettings);
            builder.AddEnvironmentVariables();
            var config = builder.Build();

         // var mgmtOptions = new List<IManagementOptions> { new ActuatorManagementOptions(config) };
            app.UseHypermediaActuator(config);
            app.UseInfoActuator(config);
        }
    }
}

using Microsoft.AspNetCore.Builder;
using Steeltoe.Management.Endpoint.Discovery;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Info;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Endpoint
{
    public static class ActuatorApplicationBuilderExtensions
    {
        /// <summary>
        /// Use only one of UseCloudfoundryActuators, UseActuators o r UseAllActuators
        /// </summary>
        /// <param name="app"></param>
        public static void UseActuators(this IApplicationBuilder app)
        {
           // app.UseActuatorSecurity();
            app.UseDiscoveryActuator();

          //  app.UseInfoActuator();
            //app.UseHealthActuator();

        }
    }
}

using Autofac;
using Steeltoe.Management.EndpointOwin.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Management.EndpointAutofac.Actuators
{
    public static class DiagnosticSourceContainerBuilderExtensions
    {
        /// <summary>
        /// Register the Diagnostics source OWIN middleware
        /// </summary>
        /// <param name="container">Autofac DI <see cref="ContainerBuilder"/></param>
        public static void RegisterDiagnosticSourceMiddleware(this ContainerBuilder container)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            container.RegisterType<DiagnosticSourceOwinMiddleware>().SingleInstance();
        }
    }
}

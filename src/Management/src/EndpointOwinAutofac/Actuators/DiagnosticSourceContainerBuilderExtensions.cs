// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Steeltoe.Management.EndpointOwin.Diagnostics;
using System;

namespace Steeltoe.Management.EndpointOwinAutofac.Actuators
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

            container.RegisterType<DiagnosticSourceOwinMiddleware>().IfNotRegistered(typeof(DiagnosticSourceOwinMiddleware)).SingleInstance();
        }
    }
}

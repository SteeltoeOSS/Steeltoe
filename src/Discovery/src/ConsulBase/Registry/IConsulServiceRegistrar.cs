// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Discovery.Consul.Registry
{
    public interface IConsulServiceRegistrar : IServiceRegistrar
    {
        /// <summary>
        /// Gets the registration that the registrar is to register with Consul
        /// </summary>
        IConsulRegistration Registration { get; }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Routing;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Mappings;

public class RouteMappings : IRouteMappings
{
    public RouteMappings()
    {
        Routers = new List<IRouter>();
    }

    public IList<IRouter> Routers { get; }
}

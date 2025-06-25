// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappings.ResponseTypes;

public sealed class RouteDispatcherServlets
{
    [JsonPropertyName("dispatcherServlet")]
    public IList<RouteDescriptor> DispatcherServlet { get; }

    public RouteDispatcherServlets(IList<RouteDescriptor> dispatcherServlet)
    {
        ArgumentNullException.ThrowIfNull(dispatcherServlet);
        ArgumentGuard.ElementsNotNull(dispatcherServlet);

        DispatcherServlet = dispatcherServlet;
    }
}

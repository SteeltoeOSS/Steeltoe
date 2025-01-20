// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Routing;

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappingsNew;

internal sealed class RouteBuilderSupplier
{
    public IRouteBuilder? RouteBuilder { get; set; }
}

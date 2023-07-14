// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.RouteMappings;

public class AspNetCoreRouteDetails
{
    public IList<string> HttpMethods { get; internal set; }

    public string RouteTemplate { get; internal set; }

    public IList<string> Produces { get; internal set; }

    public IList<string> Consumes { get; internal set; }
}

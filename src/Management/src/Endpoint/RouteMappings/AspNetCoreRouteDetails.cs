// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.RouteMappings;

public sealed class AspNetCoreRouteDetails
{
    public IList<string> HttpMethods { get; }
    public string RouteTemplate { get; }
    public IList<string> Produces { get; }
    public IList<string> Consumes { get; }

    public AspNetCoreRouteDetails(IList<string> httpMethods, string routeTemplate, IList<string> produces, IList<string> consumes)
    {
        ArgumentGuard.NotNull(httpMethods);
        ArgumentGuard.NotNull(routeTemplate);
        ArgumentGuard.NotNull(produces);
        ArgumentGuard.NotNull(consumes);

        HttpMethods = httpMethods;
        RouteTemplate = routeTemplate;
        Produces = produces;
        Consumes = consumes;
    }
}

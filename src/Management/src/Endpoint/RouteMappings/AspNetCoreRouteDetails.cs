// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.RouteMappings;

public sealed class AspNetCoreRouteDetails
{
    public string RouteTemplate { get; }
    public IList<string> HttpMethods { get; }
    public IList<string> Consumes { get; }
    public IList<string> Produces { get; }
    public IList<string> Headers { get; }
    public IList<string> Params { get; }

    public AspNetCoreRouteDetails(string routeTemplate, IList<string> httpMethods, IList<string> consumes, IList<string> produces, IList<string> headers,
        IList<string> @params)
    {
        ArgumentGuard.NotNull(routeTemplate);
        ArgumentGuard.NotNull(httpMethods);
        ArgumentGuard.NotNull(consumes);
        ArgumentGuard.NotNull(produces);
        ArgumentGuard.NotNull(headers);
        ArgumentGuard.NotNull(@params);

        RouteTemplate = routeTemplate;
        HttpMethods = httpMethods;
        Consumes = consumes;
        Produces = produces;
        Headers = headers;
        Params = @params;
    }
}

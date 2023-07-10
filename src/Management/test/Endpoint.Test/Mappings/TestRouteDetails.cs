// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.RouteMappings;

namespace Steeltoe.Management.Endpoint.Test.Mappings;

public class TestRouteDetails : IRouteDetails
{
    public IList<string> HttpMethods { get; set; }

    public string RouteTemplate { get; set; }

    public IList<string> Produces { get; set; }

    public IList<string> Consumes { get; set; }
}

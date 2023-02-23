// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Steeltoe.Management.Endpoint.Middleware;
internal class ActuatorRouter 
{
    private readonly Dictionary<string, IEndpointMiddleware> _routes = new();

    public ActuatorRouter(IOptionsMonitor<ManagementEndpointOptions> mgmtOptions, IEnumerable<IEndpointMiddleware> middlewares)
    {
        foreach (var mgmtOptionName in ManagementEndpointOptions.NamedOptionNames)
        {
            var mgmtOption = mgmtOptions.Get(mgmtOptionName);
            foreach (var middleware in middlewares)
            {
                var endpointOptions = middleware.EndpointOptions;
                var key = mgmtOption.Path + "/" + endpointOptions.EndpointSharedOptions.Path;
                if (!_routes.ContainsKey(key))
                {
                    _routes.Add(key, middleware);
                }
            }
        }
    }

    public Task RouteAsync(HttpContext context, RequestDelegate next)
    {
        var matchedPath = _routes.Keys.FirstOrDefault(k => Regex.IsMatch(context.Request.Path, k));

        if (matchedPath != null)
        {
            return _routes[matchedPath].InvokeAsync(context);
        }

        return next(context);

    }
}

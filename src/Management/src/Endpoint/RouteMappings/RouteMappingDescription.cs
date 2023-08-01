// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.RouteMappings;

public sealed class RouteMappingDescription
{
    internal const string AllHttpMethods = "GET || PUT || POST || DELETE || HEAD || OPTIONS";

    [JsonPropertyName("handler")]
    public string Handler { get; }

    [JsonPropertyName("predicate")]
    public string Predicate { get; }

    [JsonPropertyName("details")]
    public RouteMappingDetails? Details { get; }

    public RouteMappingDescription(string routeHandler, AspNetCoreRouteDetails routeDetails)
    {
        ArgumentGuard.NotNull(routeHandler);
        ArgumentGuard.NotNull(routeDetails);

        Handler = routeHandler;
        Predicate = CreatePredicateString(routeDetails);
    }

    public RouteMappingDescription(MethodInfo routeHandlerMethod, AspNetCoreRouteDetails routeDetails)
    {
        ArgumentGuard.NotNull(routeHandlerMethod);
        ArgumentGuard.NotNull(routeDetails);

        Handler = routeHandlerMethod.ToString()!;
        Predicate = CreatePredicateString(routeDetails);
        Details = CreateMappingDetails(routeDetails);
    }

    private string CreatePredicateString(AspNetCoreRouteDetails routeDetails)
    {
        var builder = new StringBuilder("{");

        builder.Append($"[{routeDetails.RouteTemplate}]");

        builder.Append(",methods=");
        builder.Append($"[{CreateRouteMethods(routeDetails.HttpMethods)}]");

        if (!IsNullOrEmpty(routeDetails.Produces))
        {
            builder.Append(",produces=");
            builder.Append($"[{string.Join(" || ", routeDetails.Produces)}]");
        }

        if (!IsNullOrEmpty(routeDetails.Consumes))
        {
            builder.Append(",consumes=");
            builder.Append($"[{string.Join(" || ", routeDetails.Consumes)}]");
        }

        builder.Append('}');
        return builder.ToString();
    }

    private string CreateRouteMethods(IList<string> httpMethods)
    {
        if (IsNullOrEmpty(httpMethods))
        {
            return AllHttpMethods;
        }

        return string.Join(" || ", httpMethods);
    }

    private RouteMappingDetails CreateMappingDetails(AspNetCoreRouteDetails routeDetails)
    {
        List<MediaTypeDescriptor> consumes = routeDetails.Consumes.Select(consumes => new MediaTypeDescriptor(consumes, false)).ToList();
        List<MediaTypeDescriptor> produces = routeDetails.Produces.Select(produces => new MediaTypeDescriptor(produces, false)).ToList();

        var patterns = new List<string>
        {
            routeDetails.RouteTemplate
        };

        var conditions = new RequestMappingConditions(consumes, produces, new List<string>(), routeDetails.HttpMethods.ToList(), patterns);
        return new RouteMappingDetails(conditions);
    }

    private static bool IsNullOrEmpty(ICollection<string>? list)
    {
        if (list == null || list.Count == 0)
        {
            return true;
        }

        return false;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Mappings;

public class MappingDescription
{
    public const string AllHttpMethods = "GET || PUT || POST || DELETE || HEAD || OPTIONS";

    [JsonPropertyName("handler")]
    public string Handler { get; }

    [JsonPropertyName("predicate")]
    public string Predicate { get; }

    [JsonPropertyName("details")]
    public RouteMappingDetails Details { get; }

    public MappingDescription(string routeHandler, IRouteDetails routeDetails)
    {
        ArgumentGuard.NotNull(routeDetails);
        ArgumentGuard.NotNull(routeHandler);

        Predicate = CreatePredicateString(routeDetails);
        Handler = routeHandler;
    }

    public MappingDescription(MethodInfo routeHandler, IRouteDetails routeDetails)
    {
        ArgumentGuard.NotNull(routeHandler);
        ArgumentGuard.NotNull(routeDetails);

        Predicate = CreatePredicateString(routeDetails);
        Handler = CreateHandlerString(routeHandler);
        Details = CreateMappingDetails(routeDetails);
    }

    private RouteMappingDetails CreateMappingDetails(IRouteDetails routeDetails)
    {
        return new RouteMappingDetails
        {
            RequestMappingConditions = new RequestMappingConditions
            {
                Consumes = routeDetails.Consumes.Select(consumes => new MediaTypeDescriptor
                {
                    MediaType = consumes
                }).ToList(),
                Produces = routeDetails.Produces.Select(produces => new MediaTypeDescriptor
                {
                    MediaType = produces
                }).ToList(),
                Methods = routeDetails.HttpMethods.ToList(),
                Patterns = new List<string>
                {
                    routeDetails.RouteTemplate
                },
                Params = new List<string>(), // Does not apply for .NET
                Headers = new List<string>() // Cannot infer here for .NET 
            }
        };
    }

    private string CreateHandlerString(MethodInfo actionHandlerMethod)
    {
        return actionHandlerMethod.ToString();
    }

    private string CreatePredicateString(IRouteDetails routeDetails)
    {
        var sb = new StringBuilder("{");

        sb.Append($"[{routeDetails.RouteTemplate}]");

        sb.Append(",methods=");
        sb.Append($"[{CreateRouteMethods(routeDetails.HttpMethods)}]");

        if (!IsEmpty(routeDetails.Produces))
        {
            sb.Append(",produces=");
            sb.Append($"[{string.Join(" || ", routeDetails.Produces)}]");
        }

        if (!IsEmpty(routeDetails.Consumes))
        {
            sb.Append(",consumes=");
            sb.Append($"[{string.Join(" || ", routeDetails.Consumes)}]");
        }

        sb.Append('}');
        return sb.ToString();
    }

    private string CreateRouteMethods(IList<string> httpMethods)
    {
        if (IsEmpty(httpMethods))
        {
            return AllHttpMethods;
        }

        return string.Join(" || ", httpMethods);
    }

    private bool IsEmpty(IList<string> list)
    {
        if (list == null || list.Count == 0)
        {
            return true;
        }

        return false;
    }
}

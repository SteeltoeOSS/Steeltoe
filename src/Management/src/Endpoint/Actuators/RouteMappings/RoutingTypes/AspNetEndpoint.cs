// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

#pragma warning disable S107 // Methods should not have too many parameters

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappings.RoutingTypes;

/// <summary>
/// Provides unified information about an ASP.NET Core endpoint.
/// </summary>
internal sealed class AspNetEndpoint
{
    private readonly AspNetEndpointSource _source;

    public string DisplayName { get; }
    public string? RoutePattern { get; }
    public IReadOnlySet<string> HttpMethods { get; }
    public MethodInfo? HandlerMethod { get; }
    public IReadOnlyList<AspNetEndpointParameter> Parameters { get; }
    public IReadOnlyList<AspNetEndpointParameter> Headers { get; }
    public IReadOnlySet<string> Consumes { get; }
    public IReadOnlySet<string> Produces { get; }

    public AspNetEndpoint(AspNetEndpointSource source, string displayName, string? routePattern, List<string> httpMethods, MethodInfo? handlerMethod,
        List<AspNetEndpointParameter> parameters, List<string> consumes, List<string> produces)
    {
        ArgumentNullException.ThrowIfNull(displayName);
        ArgumentNullException.ThrowIfNull(httpMethods);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(consumes);
        ArgumentNullException.ThrowIfNull(produces);

        _source = source;
        DisplayName = displayName;
        RoutePattern = routePattern;
        HttpMethods = ExplodeHttpMethods(httpMethods);
        HandlerMethod = handlerMethod;
        Parameters = FilterParameters(parameters).ToList().AsReadOnly();
        Headers = parameters.Where(parameter => parameter.Origin is AspNetEndpointParameterOrigin.Header).ToList().AsReadOnly();
        Consumes = consumes.ToHashSet();
        Produces = produces.ToHashSet();
    }

    private static IEnumerable<AspNetEndpointParameter> FilterParameters(List<AspNetEndpointParameter> parameters)
    {
        // Include parameters from query string and unknown origin for diagnostics purposes.
        return parameters.Where(parameter =>
            parameter.Origin is AspNetEndpointParameterOrigin.Route or AspNetEndpointParameterOrigin.QueryString or AspNetEndpointParameterOrigin.Unknown);
    }

    internal static HashSet<string> ExplodeHttpMethods(ICollection<string> methods)
    {
        return methods.Count == 0
            ?
            [
                "GET",
                "HEAD",
                "POST",
                "PUT",
                "DELETE",
                "OPTIONS",
                "PATCH"
            ]
            : methods.Select(method => method.ToUpperInvariant()).ToHashSet();
    }

    public override string ToString()
    {
        string? name = string.IsNullOrWhiteSpace(DisplayName) ? RoutePattern : DisplayName;
        return $"{_source}: {name}";
    }
}

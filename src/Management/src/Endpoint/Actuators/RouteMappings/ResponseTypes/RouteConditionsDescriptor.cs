// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings.RoutingTypes;

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappings.ResponseTypes;

public sealed class RouteConditionsDescriptor
{
    [JsonPropertyName("patterns")]
    public IList<string> Patterns { get; }

    [JsonPropertyName("methods")]
    public IList<string> Methods { get; }

    [JsonPropertyName("consumes")]
    public IList<MediaTypeDescriptor> Consumes { get; }

    [JsonPropertyName("produces")]
    public IList<MediaTypeDescriptor> Produces { get; }

    [JsonPropertyName("headers")]
    public IList<ParameterDescriptor> Headers { get; }

    [JsonPropertyName("params")]
    public IList<ParameterDescriptor> Parameters { get; }

    public RouteConditionsDescriptor(IList<string> patterns, IList<string> methods, IList<MediaTypeDescriptor> consumes, IList<MediaTypeDescriptor> produces,
        IList<ParameterDescriptor> headers, IList<ParameterDescriptor> parameters)
    {
        ArgumentNullException.ThrowIfNull(patterns);
        ArgumentGuard.ElementsNotNull(patterns);
        ArgumentNullException.ThrowIfNull(methods);
        ArgumentGuard.ElementsNotNullOrWhiteSpace(methods);
        ArgumentNullException.ThrowIfNull(consumes);
        ArgumentGuard.ElementsNotNull(consumes);
        ArgumentNullException.ThrowIfNull(produces);
        ArgumentGuard.ElementsNotNull(produces);
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentGuard.ElementsNotNull(headers);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentGuard.ElementsNotNull(parameters);

        Patterns = patterns;
        Methods = methods;
        Consumes = consumes;
        Produces = produces;
        Headers = headers;
        Parameters = parameters;
    }

    internal static RouteConditionsDescriptor FromAspNetEndpoint(AspNetEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        List<string> patterns = [endpoint.RoutePattern ?? string.Empty];
        List<string> methods = endpoint.HttpMethods.ToList();
        List<MediaTypeDescriptor> consumes = endpoint.Consumes.Select(mediaType => new MediaTypeDescriptor(mediaType)).ToList();
        List<MediaTypeDescriptor> produces = endpoint.Produces.Select(mediaType => new MediaTypeDescriptor(mediaType)).ToList();
        List<ParameterDescriptor> parameters = endpoint.Parameters.Select(ParameterDescriptor.FromAspNetEndpointParameter).ToList();
        List<ParameterDescriptor> headers = endpoint.Headers.Select(ParameterDescriptor.FromAspNetEndpointParameter).ToList();

        return new RouteConditionsDescriptor(patterns, methods, consumes, produces, headers, parameters);
    }
}

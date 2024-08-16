// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.RouteMappings;

public sealed class RequestMappingConditions
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
    public IList<string> Headers { get; }

    [JsonPropertyName("params")]
    public IList<string> Params { get; }

    public RequestMappingConditions(IList<string> patterns, IList<string> methods, IList<MediaTypeDescriptor> consumes, IList<MediaTypeDescriptor> produces,
        IList<string> headers, IList<string> @params)
    {
        ArgumentNullException.ThrowIfNull(patterns);
        ArgumentGuard.ElementsNotNullOrWhiteSpace(patterns);
        ArgumentNullException.ThrowIfNull(methods);
        ArgumentGuard.ElementsNotNullOrWhiteSpace(methods);
        ArgumentNullException.ThrowIfNull(consumes);
        ArgumentGuard.ElementsNotNull(consumes);
        ArgumentNullException.ThrowIfNull(produces);
        ArgumentGuard.ElementsNotNull(produces);
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentGuard.ElementsNotNullOrWhiteSpace(headers);
        ArgumentNullException.ThrowIfNull(@params);
        ArgumentGuard.ElementsNotNullOrWhiteSpace(@params);

        Patterns = patterns;
        Methods = methods;
        Consumes = consumes;
        Produces = produces;
        Headers = headers;
        Params = @params;
    }
}

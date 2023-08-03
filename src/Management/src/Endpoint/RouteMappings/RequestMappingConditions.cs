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
        ArgumentGuard.NotNull(patterns);
        ArgumentGuard.NotNull(methods);
        ArgumentGuard.NotNull(consumes);
        ArgumentGuard.NotNull(produces);
        ArgumentGuard.NotNull(headers);
        ArgumentGuard.NotNull(@params);

        Patterns = patterns;
        Methods = methods;
        Consumes = consumes;
        Produces = produces;
        Headers = headers;
        Params = @params;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.RouteMappings;

public sealed class RequestMappingConditions
{
    [JsonPropertyName("consumes")]
    public IList<MediaTypeDescriptor> Consumes { get; }

    [JsonPropertyName("produces")]
    public IList<MediaTypeDescriptor> Produces { get; }

    [JsonPropertyName("headers")]
    public IList<string> Headers { get; }

    [JsonPropertyName("methods")]
    public IList<string> Methods { get; }

    [JsonPropertyName("patterns")]
    public IList<string> Patterns { get; }

    public RequestMappingConditions(IList<MediaTypeDescriptor> consumes, IList<MediaTypeDescriptor> produces, IList<string> headers, IList<string> methods,
        IList<string> patterns)
    {
        ArgumentGuard.NotNull(consumes);
        ArgumentGuard.NotNull(produces);
        ArgumentGuard.NotNull(headers);
        ArgumentGuard.NotNull(methods);
        ArgumentGuard.NotNull(patterns);

        Consumes = consumes;
        Produces = produces;
        Headers = headers;
        Methods = methods;
        Patterns = patterns;
    }
}

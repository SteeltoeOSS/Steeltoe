// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappings.ResponseTypes;

public sealed class RouteHandlerDescriptor
{
    [JsonPropertyName("className")]
    public string ClassName { get; }

    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("descriptor")]
    public string Descriptor { get; }

    public RouteHandlerDescriptor(MethodInfo methodInfo)
    {
        ArgumentNullException.ThrowIfNull(methodInfo);

        ClassName = (methodInfo.ReflectedType ?? methodInfo.DeclaringType)?.FullName!;
        Name = methodInfo.Name;
        Descriptor = methodInfo.ToString()!;
    }
}

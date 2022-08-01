// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Integration.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class EndpointIdAttribute : Attribute
{
    public EndpointIdAttribute()
    {
    }

    public EndpointIdAttribute(string id)
    {
        Id = id;
    }

    public string Id { get; }
}

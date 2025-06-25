// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.CloudFoundry.ServiceBindings;

internal sealed class StringServiceBindingsReader(string? json) : IServiceBindingsReader
{
    private readonly string? _json = json;

    public string? GetServiceBindingsJson()
    {
        return _json;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding;

internal sealed class StringServiceBindingsReader : IServiceBindingsReader
{
    private readonly string? _json;

    public StringServiceBindingsReader(string? json)
    {
        _json = json;
    }

    public string? GetServiceBindingsJson()
    {
        return _json;
    }
}

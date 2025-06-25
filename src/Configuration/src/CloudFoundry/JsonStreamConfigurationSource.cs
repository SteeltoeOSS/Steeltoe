// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace Steeltoe.Configuration.CloudFoundry;

internal sealed class JsonStreamConfigurationSource : JsonConfigurationSource
{
    internal Stream Stream { get; }

    public JsonStreamConfigurationSource(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        Stream = stream;
    }

    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return new JsonStreamConfigurationProvider(this);
    }
}

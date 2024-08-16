// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration.Json;

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding;

internal sealed class JsonStreamConfigurationProvider : JsonConfigurationProvider
{
    private readonly JsonStreamConfigurationSource _source;

    public JsonStreamConfigurationProvider(JsonStreamConfigurationSource source)
        : base(source)
    {
        ArgumentNullException.ThrowIfNull(source);

        _source = source;
    }

    public override void Load()
    {
        Load(_source.Stream);
    }
}

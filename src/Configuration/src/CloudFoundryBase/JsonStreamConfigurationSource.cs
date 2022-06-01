// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System;
using System.IO;

namespace Steeltoe.Extensions.Configuration.CloudFoundry;

internal class JsonStreamConfigurationSource : JsonConfigurationSource
{
    internal JsonStreamConfigurationSource(MemoryStream stream)
    {
        Stream = stream ?? throw new ArgumentNullException(nameof(stream));
    }

    internal MemoryStream Stream { get; }

    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new JsonStreamConfigurationProvider(this);
    }
}

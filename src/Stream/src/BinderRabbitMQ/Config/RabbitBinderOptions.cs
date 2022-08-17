// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IO.Compression;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Stream.Binder.Rabbit.Config;

public class RabbitBinderOptions
{
    public const string Prefix = "spring:cloud:stream:rabbit:binder";

    public List<string> AdminAddresses { get; set; } = new();

    public List<string> Nodes { get; set; } = new();

    public CompressionLevel CompressionLevel { get; set; }

    public string ConnectionNamePrefix { get; set; }

    public RabbitBinderOptions()
    {
    }

    internal RabbitBinderOptions(IConfiguration config)
    {
        ArgumentGuard.NotNull(config);

        config.Bind(this);
    }
}

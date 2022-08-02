// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IO.Compression;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Stream.Binder.Rabbit.Config;

public class RabbitBinderOptionsTest
{
    [Fact]
    public void InitializeAll_FromConfigValues()
    {
        var builder = new ConfigurationBuilder();

        builder.AddInMemoryCollection(new Dictionary<string, string>
        {
            { "spring:cloud:stream:rabbit:binder:adminAddresses:0", "adminAddresses0" },
            { "spring:cloud:stream:rabbit:binder:adminAddresses:1", "adminAddresses1" },
            { "spring:cloud:stream:rabbit:binder:nodes:0", "nodes0" },
            { "spring:cloud:stream:rabbit:binder:nodes:1", "nodes1" },
            { "spring:cloud:stream:rabbit:binder:compressionLevel", "NoCompression" },
            { "spring:cloud:stream:rabbit:binder:connectionNamePrefix", "connectionNamePrefix" }
        });

        IConfigurationSection config = builder.Build().GetSection("spring:cloud:stream:rabbit:binder");
        var options = new RabbitBinderOptions(config);

        Assert.Equal("adminAddresses0", options.AdminAddresses[0]);
        Assert.Equal("adminAddresses1", options.AdminAddresses[1]);
        Assert.Equal("nodes0", options.Nodes[0]);
        Assert.Equal("nodes1", options.Nodes[1]);
        Assert.Equal(CompressionLevel.NoCompression, options.CompressionLevel);
        Assert.Equal("connectionNamePrefix", options.ConnectionNamePrefix);
    }
}

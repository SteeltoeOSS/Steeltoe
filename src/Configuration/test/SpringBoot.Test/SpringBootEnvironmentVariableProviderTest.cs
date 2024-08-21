// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;

namespace Steeltoe.Configuration.SpringBoot.Test;

public sealed class SpringBootEnvironmentVariableProviderTest
{
    [Fact]
    public void TryGet_Flat()
    {
        var provider = new SpringBootEnvironmentVariableProvider("""
            {
              "management.metrics.tags.application.type": "${spring.cloud.dataflow.stream.app.type:unknown}"
            }
            """);

        provider.Load();
        provider.TryGet("management:metrics:tags:application:type", out string? value);

        Assert.NotNull(value);
        Assert.Equal("${spring.cloud.dataflow.stream.app.type:unknown}", value);
    }

    [Fact]
    public void TryGet_Tree()
    {
        const string configString = """
            {
              "a.b.c": {
                "e.f": {
                  "g": "h",
                  "i.j": "k"
                }
              },
              "l.m.n": "o",
              "p": null
            }
            """;

        var provider = new SpringBootEnvironmentVariableProvider(configString);

        provider.Load();
        provider.TryGet("a:b:c:e:f:i:j", out string? value);
        Assert.NotNull(value);
        Assert.Equal("k", value);
        provider.TryGet("l:m:n", out value);
        Assert.NotNull(value);
        Assert.Equal("o", value);
    }

    [Fact]
    public void Provider_Throws_For_Malformed()
    {
        var provider = new SpringBootEnvironmentVariableProvider("""{"a":}""");

        Assert.ThrowsAny<JsonException>(() => provider.Load());
    }
}

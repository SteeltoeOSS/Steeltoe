// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Xunit;

namespace Steeltoe.Extensions.Configuration.SpringBoot.Test;

public class SpringBootEnvProviderTest
{
    [Fact]
    public void TryGet_Flat()
    {
        var prov = new SpringBootEnvProvider("{\"management.metrics.tags.application.type\":\"${spring.cloud.dataflow.stream.app.type:unknown}\"}");
        prov.Load();
        prov.TryGet("management:metrics:tags:application:type", out string value);
        Assert.NotNull(value);
        Assert.Equal("${spring.cloud.dataflow.stream.app.type:unknown}", value);
    }

    [Fact]
    public void TryGet_Tree()
    {
        string configString = @"{
                    ""a.b.c"": {
                                    ""e.f"": {
                                            ""g"":""h"",
                                            ""i.j"":""k""
                                            }
                                },
                    ""l.m.n"": ""o"",
                    ""p"": null
                }";

        var prov = new SpringBootEnvProvider(configString);

        prov.Load();
        prov.TryGet("a:b:c:e:f:i:j", out string value);
        Assert.NotNull(value);
        Assert.Equal("k", value);
        prov.TryGet("l:m:n", out value);
        Assert.NotNull(value);
        Assert.Equal("o", value);
    }

    [Fact]
    public void Provider_Throws_For_Malformed()
    {
        var prov = new SpringBootEnvProvider("{\"a\":}");

        Assert.ThrowsAny<JsonException>(() => prov.Load());
    }
}

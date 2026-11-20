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

        provider.TryGet("management:metrics:tags:application:type", out string? value).Should().BeTrue();
        value.Should().Be("${spring.cloud.dataflow.stream.app.type:unknown}");

        provider.TryGet("management.metrics.tags.application.type", out _).Should().BeFalse();
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
              "p": "q",
              "r": null,
              "s.t": null,
              "u": {
              },
              "v.w": {
              }
            }
            """;

        var provider = new SpringBootEnvironmentVariableProvider(configString);
        provider.Load();

        provider.TryGet("a:b:c:e:f:g", out string? value).Should().BeTrue();
        value.Should().Be("h");

        provider.TryGet("a:b:c:e:f:i:j", out value).Should().BeTrue();
        value.Should().Be("k");

        provider.TryGet("l:m:n", out value).Should().BeTrue();
        value.Should().Be("o");

        provider.TryGet("p", out value).Should().BeTrue();
        value.Should().Be("q");

        provider.TryGet("r", out value).Should().BeTrue();
        value.Should().BeNull();

        provider.TryGet("s:t", out value).Should().BeTrue();
        value.Should().BeNull();

        provider.TryGet("u", out value).Should().BeTrue();
        value.Should().BeNull();

        provider.TryGet("v:w", out value).Should().BeTrue();
        value.Should().BeNull();
    }

    [Fact]
    public void TryGet_Array()
    {
        const string configString = """
            {
              "a.b.c": [{
                "e.f": {
                  "g": "h",
                  "i.j": "k"
                }
              }, {
                "l.m.n": "o",
                "p": "q"
              }, {
                "r": null,
                "s.t": null,
                "u": {
                },
                "v.w": {
                }
              }]
            }
            """;

        var provider = new SpringBootEnvironmentVariableProvider(configString);
        provider.Load();

        provider.TryGet("a:b:c:0:e:f:g", out string? value).Should().BeTrue();
        value.Should().Be("h");

        provider.TryGet("a:b:c:0:e:f:i:j", out value).Should().BeTrue();
        value.Should().Be("k");

        provider.TryGet("a:b:c:1:l:m:n", out value).Should().BeTrue();
        value.Should().Be("o");

        provider.TryGet("a:b:c:1:p", out value).Should().BeTrue();
        value.Should().Be("q");

        provider.TryGet("a:b:c:2:r", out value).Should().BeTrue();
        value.Should().BeNull();

        provider.TryGet("a:b:c:2:s:t", out value).Should().BeTrue();
        value.Should().BeNull();

        provider.TryGet("a:b:c:2:u", out value).Should().BeTrue();
        value.Should().BeNull();

        provider.TryGet("a:b:c:2:v:w", out value).Should().BeTrue();
        value.Should().BeNull();
    }

    [Fact]
    public void TryGet_KeysWithBrackets()
    {
        const string configString = """
            {
              "a.b[0].c": "d",
              "e[f.g].h": "i",
              "[j.k]": {
                "l[0]": "m",
                "n[o.p]": "q"
              },
              "logging.level[Steeltoe.Common]": "Debug"
            }
            """;

        var provider = new SpringBootEnvironmentVariableProvider(configString);
        provider.Load();

        provider.TryGet("a:b:0:c", out string? value).Should().BeTrue();
        value.Should().Be("d");

        provider.TryGet("e:f.g:h", out value).Should().BeTrue();
        value.Should().Be("i");

        provider.TryGet("j.k:l:0", out value).Should().BeTrue();
        value.Should().Be("m");

        provider.TryGet("j.k:n:o.p", out value).Should().BeTrue();
        value.Should().Be("q");

        provider.TryGet("logging:level:Steeltoe.Common", out value).Should().BeTrue();
        value.Should().Be("Debug");
    }

    [Fact]
    public void Provider_Throws_For_Malformed()
    {
        var provider = new SpringBootEnvironmentVariableProvider("""{"a":}""");

        Action action = provider.Load;
        action.Should().Throw<JsonException>();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using Xunit;

namespace Steeltoe.Extensions.Configuration.SpringBoot.Test;

public class SpringBootCmdProviderTest
{
    [Fact]
    public void TryGet_Key()
    {
        var config = new ConfigurationBuilder()
            .AddCommandLine(new[] { "spring.cloud.stream.bindings.input=test" })
            .Build();
        var prov = new SpringBootCmdProvider(config);
        prov.Load();
        prov.TryGet("spring:cloud:stream:bindings:input", out var value);
        Assert.NotNull(value);
        Assert.Equal("test", value);
    }

    [Fact]
    public void Throws_When_ArgumentsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SpringBootCmdProvider(null));
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Extensions.Configuration.SpringBoot.Test;

public sealed class SpringBootCmdProviderTest
{
    [Fact]
    public void TryGet_Key()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddCommandLine(new[]
        {
            "spring.cloud.stream.bindings.input=test"
        }).Build();

        var prov = new SpringBootCmdProvider(configurationRoot);
        prov.Load();
        prov.TryGet("spring:cloud:stream:bindings:input", out string value);
        Assert.NotNull(value);
        Assert.Equal("test", value);
    }

    [Fact]
    public void Throws_When_ArgumentsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SpringBootCmdProvider(null));
    }
}

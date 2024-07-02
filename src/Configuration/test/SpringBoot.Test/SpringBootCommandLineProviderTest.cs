// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.SpringBoot.Test;

public sealed class SpringBootCommandLineProviderTest
{
    [Fact]
    public void TryGet_Key()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddCommandLine(["spring.cloud.stream.bindings.input=test"]).Build();

        var provider = new SpringBootCommandLineProvider(configurationRoot);
        provider.Load();
        provider.TryGet("spring:cloud:stream:bindings:input", out string? value);

        Assert.NotNull(value);
        Assert.Equal("test", value);
    }
}

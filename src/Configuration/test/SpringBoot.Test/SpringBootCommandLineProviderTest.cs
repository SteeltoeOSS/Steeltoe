// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.SpringBoot.Test;

public sealed class SpringBootCommandLineProviderTest
{
    [Fact]
    public void TryGet_Key()
    {
        var provider = new SpringBootCommandLineProvider(["spring.cloud.stream.bindings.input=test"]);
        provider.Load();
        provider.TryGet("spring:cloud:stream:bindings:input", out string? value);

        value.Should().Be("test");
    }
}

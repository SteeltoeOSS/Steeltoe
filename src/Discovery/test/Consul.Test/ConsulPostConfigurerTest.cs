// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Discovery.Consul.Test;

public sealed class ConsulPostConfigurerTest
{
    [Fact]
    public void ValidateOptionsComplainsAboutDefaultWhenWontWork()
    {
        using var scope = new EnvironmentVariableScope("DOTNET_RUNNING_IN_CONTAINER", "true");

        var exception = Assert.Throws<InvalidOperationException>(() => ConsulPostConfigurer.ValidateConsulOptions(new ConsulOptions()));
        Assert.Contains("localhost", exception.Message, StringComparison.Ordinal);
    }
}

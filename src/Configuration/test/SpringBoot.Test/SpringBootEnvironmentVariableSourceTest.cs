// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Extensions.Configuration.SpringBoot.Test;

public sealed class SpringBootEnvironmentVariableSourceTest
{
    [Fact]
    public void Build__ReturnsProvider()
    {
        var source = new SpringBootEnvironmentVariableSource();
        IConfigurationProvider provider = source.Build(new ConfigurationBuilder());
        Assert.IsType<SpringBootEnvironmentVariableProvider>(provider);
    }
}

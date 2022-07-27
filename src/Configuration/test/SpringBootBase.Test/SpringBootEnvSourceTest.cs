// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Steeltoe.Extensions.Configuration.SpringBoot.Test;

public class SpringBootEnvSourceTest
{
    [Fact]
    public void Build__ReturnsProvider()
    {
        ILoggerFactory factory = new LoggerFactory();

        var source = new SpringBootEnvSource();
        var provider = source.Build(new ConfigurationBuilder());
        Assert.IsType<SpringBootEnvProvider>(provider);
    }
}
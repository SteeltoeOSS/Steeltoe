// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Extensions.Configuration.SpringBoot.Test;

public class SpringBootConfigurationBuilderExtensionsTest
{
    [Fact]
    public void AddSpringBootEnv_ThrowsIfConfigBuilderNull()
    {
        const IConfigurationBuilder configurationBuilder = null;

        Assert.Throws<ArgumentNullException>(() => configurationBuilder.AddSpringBootEnv());
    }

    [Fact]
    public void AddSpringBootEnv_AddKeys()
    {
        Environment.SetEnvironmentVariable("SPRING_APPLICATION_JSON", "{\"foo.bar\":\"value\"}");

        IConfigurationBuilder builder = new ConfigurationBuilder().AddSpringBootEnv();
        IConfigurationRoot config = builder.Build();
        string value = config["foo:bar"];
        Assert.Equal("value", value);
    }

    [Fact]
    public void AddSpringBootCmd_ThrowsIfConfigBuilderNull()
    {
        IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();

        var ex = Assert.Throws<ArgumentNullException>(() => SpringBootConfigurationBuilderExtensions.AddSpringBootCmd(null, configurationBuilder.Build()));
        Assert.Equal("builder", ex.ParamName);
        var ex2 = Assert.Throws<ArgumentNullException>(() => configurationBuilder.AddSpringBootCmd(null));
        Assert.Equal("configuration", ex2.ParamName);
    }

    [Fact]
    public void AddSpringBootCmd_AddKeys()
    {
        IConfigurationRoot config1 = new ConfigurationBuilder().AddCommandLine(new[]
        {
            "spring.foo.bar=value",
            "spring.bar.foo=value2",
            "bar.foo=value3"
        }).Build();

        IConfigurationBuilder builder = new ConfigurationBuilder().AddSpringBootCmd(config1);

        IConfigurationRoot config = builder.Build();
        string value = config["spring:foo:bar"];
        Assert.Equal("value", value);

        value = config["spring:bar:foo"];
        Assert.Equal("value2", value);

        Assert.Null(config["bar:foo"]);
    }
}

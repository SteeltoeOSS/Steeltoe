// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Configuration.SpringBoot.Test;

public sealed class SpringBootConfigurationBuilderExtensionsTest
{
    [Fact]
    public void AddSpringBootEnv_AddKeys()
    {
        using var scope = new EnvironmentVariableScope("SPRING_APPLICATION_JSON", "{\"foo.bar\":\"value\"}");

        IConfigurationBuilder builder = new ConfigurationBuilder().AddSpringBootFromEnvironmentVariable();
        IConfigurationRoot configurationRoot = builder.Build();
        string? value = configurationRoot["foo:bar"];
        Assert.Equal("value", value);
    }

    [Fact]
    public void AddSpringBootCmd_AddKeys()
    {
        IConfigurationRoot configuration1 = new ConfigurationBuilder().AddCommandLine([
            "spring.foo.bar=value",
            "spring.bar.foo=value2",
            "bar.foo=value3"
        ]).Build();

        IConfigurationBuilder builder = new ConfigurationBuilder().AddSpringBootFromCommandLine(configuration1);

        IConfigurationRoot configuration2 = builder.Build();
        string? value = configuration2["spring:foo:bar"];

        Assert.Equal("value", value);

        value = configuration2["spring:bar:foo"];
        Assert.Equal("value2", value);

        Assert.Null(configuration2["bar:foo"]);
    }
}

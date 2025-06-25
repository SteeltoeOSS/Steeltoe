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
        string[] args =
        [
            "spring.foo.bar=value",
            "spring.bar[0].foo=value2",
            "bar.foo=value3"
        ];

        IConfigurationBuilder builder = new ConfigurationBuilder().AddSpringBootFromCommandLine(args);

        IConfigurationRoot configuration = builder.Build();
        string? value = configuration["spring:foo:bar"];

        Assert.Equal("value", value);

        value = configuration["spring:bar:0:foo"];
        Assert.Equal("value2", value);

        Assert.Null(configuration["bar:foo"]);
    }
}

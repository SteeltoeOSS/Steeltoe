// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Steeltoe.Bootstrap.AutoConfiguration;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Bootstrap.EmptyAutoConfiguration.Test;

public sealed class EmptyAutoConfigurationTest
{
    [Fact]
    public void Bootstrap_does_not_depend_on_other_Steeltoe_packages()
    {
        // Assemblies from referenced projects without PrivateAssets="All" are copied into the test output directory during build.
        var whitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Steeltoe.Bootstrap.AutoConfiguration.dll",
            "Steeltoe.Common.dll",
            "Steeltoe.Common.Hosting.dll",
            "Steeltoe.Common.Logging.dll",

            "Steeltoe.Bootstrap.EmptyAutoConfiguration.Test.dll",
            "Steeltoe.Common.TestResources.dll"
        };

        string[] files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "Steeltoe*.dll").Select(Path.GetFileName).Select(fileName => fileName!)
            .Where(fileName => !whitelist.Contains(fileName)).ToArray();

        files.Should().BeEmpty();
    }

    [Fact]
    public void Loads_without_any_Steeltoe_references_using_WebApplicationBuilder()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();

        Action action = () => builder.AddSteeltoe();

        action.Should().NotThrow();
    }

    [Fact]
    public void Loads_without_any_Steeltoe_references_using_HostApplicationBuilder()
    {
        HostApplicationBuilder builder = TestHostApplicationBuilderFactory.Create();

        Action action = () => builder.AddSteeltoe();

        action.Should().NotThrow();
    }

    [Fact]
    public void Loads_without_any_Steeltoe_references_using_WebHostBuilder()
    {
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();

        Action action = () => builder.AddSteeltoe();

        action.Should().NotThrow();
    }

    [Fact]
    public void Loads_without_any_Steeltoe_references_using_HostBuilder()
    {
        IHostBuilder builder = TestHostBuilderFactory.Create();

        Action action = () => builder.AddSteeltoe();

        action.Should().NotThrow();
    }
}

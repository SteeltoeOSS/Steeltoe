// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.Test;

public sealed class ServiceBindingConfigurationSourceTest
{
    [Fact]
    public void EnvironmentVariableNotSet()
    {
        // Not optional, should throw
        var source = new ServiceBindingConfigurationSource();
        Assert.Null(source.ServiceBindingRoot);
    }

    [Fact]
    public void EnvironmentVariableSet()
    {
        string rootDir = GetK8SResourcesDirectory(null);
        Environment.SetEnvironmentVariable(ServiceBindingConfigurationSource.ServiceBindingRootDirEnvVariable, rootDir);

        try
        {
            var source = new ServiceBindingConfigurationSource();
            Assert.Contains(Path.Combine("resources", "k8s"), source.ServiceBindingRoot, StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(source.FileProvider);
            Assert.NotNull(source.FileProvider.GetDirectoryContents("/"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(ServiceBindingConfigurationSource.ServiceBindingRootDirEnvVariable, null);
        }
    }

    [Fact]
    public void Build_CapturesParentConfiguration()
    {
        string rootDir = GetK8SResourcesDirectory(null);
        var source = new ServiceBindingConfigurationSource(rootDir);

        var builder = new ConfigurationBuilder();
        builder.Add(source);

        builder.AddInMemoryCollection(new Dictionary<string, string>
        {
            { "some:value:in:configuration:path", "true" }
        });

        builder.Build();

        source.ParentConfiguration.Should().NotBeNull();
        source.ParentConfiguration.GetValue<bool>("some:value:in:configuration:path").Should().BeTrue();
    }

    private static string GetK8SResourcesDirectory(string name)
    {
        return Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "resources", "k8s", $"{name}");
    }
}

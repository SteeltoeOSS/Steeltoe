// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Xunit;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.Test;

public sealed class ServiceBindingTest
{
    [Fact]
    public void InvalidDirectory_Throws()
    {
        Action action = () => _ = new ServiceBindingConfigurationProvider.ServiceBinding("invalid");
        action.Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void PopulatesFromFileSystem_Kubernetes()
    {
        string rootDir = GetK8SResourcesDirectory("test-name-1");
        var binding = new ServiceBindingConfigurationProvider.ServiceBinding(rootDir);
        binding.Name.Should().Be("test-name-1");
        binding.Type.Should().Be("test-type-1");
        binding.Provider.Should().Be("test-provider-1");
        binding.Path.Should().Contain(Path.Combine("resources", "k8s", "test-name-1"));
        binding.Secrets.Should().NotBeNull();
        binding.Secrets.Should().ContainSingle();
        binding.Secrets["test-secret-key"].Should().Be("test-secret-value");
    }

    [Fact]
    public void PopulatesFromFileSystem_WithHiddenFilesAndLinks_Kubernetes()
    {
        // Hidden & links
        string rootDir = GetK8SResourcesDirectory("test-k8s");
        var binding = new ServiceBindingConfigurationProvider.ServiceBinding(rootDir);
        binding.Name.Should().Be("test-k8s");
        binding.Type.Should().Be("test-type-1");
        binding.Provider.Should().Be("test-provider-1");
        binding.Path.Should().Contain(Path.Combine("resources", "k8s", "test-k8s"));
        binding.Secrets.Should().NotBeNull();
        binding.Secrets.Should().ContainSingle();
        binding.Secrets["test-secret-key"].Should().Be("test-secret-value");
    }

    private static string GetK8SResourcesDirectory(string name)
    {
        return Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "resources", "k8s", $"{name}");
    }
}

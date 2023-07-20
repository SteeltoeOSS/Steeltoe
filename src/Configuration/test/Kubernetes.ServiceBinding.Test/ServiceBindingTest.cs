// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.Test;

public sealed class ServiceBindingTest
{
    [Fact]
    public void InvalidDirectory_Throws()
    {
        string rootDirectory = GetK8SResourcesDirectory();
        var fileProvider = new PhysicalFileProvider(rootDirectory);

        Action action = () => _ = new KubernetesServiceBindingConfigurationProvider.ServiceBinding("invalid", fileProvider);
        action.Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void PopulatesFromFileSystem_Kubernetes()
    {
        string rootDirectory = GetK8SResourcesDirectory();
        var fileProvider = new PhysicalFileProvider(rootDirectory);
        var binding = new KubernetesServiceBindingConfigurationProvider.ServiceBinding("test-name-1", fileProvider);

        binding.Name.Should().Be("test-name-1");
        binding.Type.Should().Be("test-type-1");
        binding.Provider.Should().Be("test-provider-1");
        binding.Path.Should().Be("test-name-1");
        binding.Secrets.Should().ContainSingle();
        binding.Secrets.Should().Contain("test-secret-key", "test-secret-value");
    }

    [Fact]
    public void PopulatesFromFileSystem_WithHiddenFilesAndLinks_Kubernetes()
    {
        string rootDirectory = GetK8SResourcesDirectory();
        var fileProvider = new PhysicalFileProvider(rootDirectory);
        var binding = new KubernetesServiceBindingConfigurationProvider.ServiceBinding("test-k8s", fileProvider);

        binding.Name.Should().Be("test-k8s");
        binding.Type.Should().Be("test-type-1");
        binding.Provider.Should().Be("test-provider-1");
        binding.Path.Should().Be("test-k8s");
        binding.Secrets.Should().ContainSingle();
        binding.Secrets.Should().Contain("test-secret-key", "test-secret-value");
    }

    private static string GetK8SResourcesDirectory()
    {
        return Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "resources", "k8s");
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.Test;

public sealed class KubernetesServiceBindingConfigurationSourceTest
{
    [Fact]
    public void EnvironmentVariableNotSet()
    {
        using var scope = new EnvironmentVariableScope(KubernetesServiceBindingConfigurationSource.ServiceBindingRootDirEnvVariable, null);
        var source = new KubernetesServiceBindingConfigurationSource();
        source.ServiceBindingRoot.Should().BeNull();
    }

    [Fact]
    public void EnvironmentVariableSet()
    {
        string rootDirectory = GetK8SResourcesDirectory(null);
        using var scope = new EnvironmentVariableScope(KubernetesServiceBindingConfigurationSource.ServiceBindingRootDirEnvVariable, rootDirectory);

        var source = new KubernetesServiceBindingConfigurationSource();
        source.ServiceBindingRoot.Should().Contain(Path.Combine("resources", "k8s"));
        source.FileProvider.Should().NotBeNull();
        source.FileProvider.GetDirectoryContents("/").Should().NotBeNull();
    }

    [Fact]
    public void Build_CapturesParentConfiguration()
    {
        string rootDirectory = GetK8SResourcesDirectory(null);
        var source = new KubernetesServiceBindingConfigurationSource(rootDirectory);

        var builder = new ConfigurationBuilder();
        builder.Add(source);

        builder.AddInMemoryCollection(new Dictionary<string, string>
        {
            { "some:value:in:configuration:path", "true" }
        });

        builder.Build();

        IConfigurationRoot parentConfiguration = source.GetParentConfiguration();
        parentConfiguration.Should().NotBeNull();
        parentConfiguration.GetValue<bool>("some:value:in:configuration:path").Should().BeTrue();
    }

    private static string GetK8SResourcesDirectory(string name)
    {
        return Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "resources", "k8s", $"{name}");
    }
}

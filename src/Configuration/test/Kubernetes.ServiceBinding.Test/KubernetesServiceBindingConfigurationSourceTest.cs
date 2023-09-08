// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.Test;

public sealed class KubernetesServiceBindingConfigurationSourceTest
{
    [Fact]
    public void EnvironmentVariableNotSet()
    {
        using var scope = new EnvironmentVariableScope(EnvironmentServiceBindingsReader.EnvironmentVariableName, null);

        var source = new KubernetesServiceBindingConfigurationSource(new EnvironmentServiceBindingsReader());

        source.FileProvider.Should().BeNull();
    }

    [Fact]
    public void EnvironmentVariableSet()
    {
        string rootDirectory = GetK8SResourcesDirectory(string.Empty);
        using var scope = new EnvironmentVariableScope(EnvironmentServiceBindingsReader.EnvironmentVariableName, rootDirectory);

        var source = new KubernetesServiceBindingConfigurationSource(new EnvironmentServiceBindingsReader());

        source.FileProvider.Should().NotBeNull();

        PhysicalFileProvider fileProvider = source.FileProvider.Should().BeOfType<PhysicalFileProvider>().Subject;
        fileProvider.Root.Should().Contain(Path.Combine("resources", "k8s"));
        fileProvider.GetDirectoryContents("/").Exists.Should().BeTrue();
    }

    [Fact]
    public void Build_CapturesParentConfiguration()
    {
        string rootDirectory = GetK8SResourcesDirectory(string.Empty);
        var source = new KubernetesServiceBindingConfigurationSource(new DirectoryServiceBindingsReader(rootDirectory));

        var builder = new ConfigurationBuilder();
        builder.Add(source);

        builder.AddInMemoryCollection(new Dictionary<string, string?>
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
        return Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "resources", "k8s", name);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Configuration.Kubernetes.ServiceBindings.Test;

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

        PhysicalFileProvider fileProvider = source.FileProvider.Should().BeOfType<PhysicalFileProvider>().Subject;
        fileProvider.Root.Should().Contain(Path.Combine("resources", "k8s"));
        fileProvider.GetDirectoryContents("/").Exists.Should().BeTrue();
    }

    [Fact]
    public void Build_CapturesParentConfiguration()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["some:value:in:configuration:path"] = "true"
        };

        string rootDirectory = GetK8SResourcesDirectory(string.Empty);
        var source = new KubernetesServiceBindingConfigurationSource(new DirectoryServiceBindingsReader(rootDirectory));

        var builder = new ConfigurationBuilder();
        builder.Add(source);
        builder.AddInMemoryCollection(appSettings);
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

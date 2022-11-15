// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.Test;
public class ServiceBindingTest
{
    [Fact]
    public void InvalidDirectory_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ServiceBindingConfigurationProvider.ServiceBinding("invalid"));
    }

    [Fact]
    public void PopulatesFromFileSystem_Kubernetes()
    {
        var rootDir = GetK8SResourcesDirectory("test-name-1");
        var binding = new ServiceBindingConfigurationProvider.ServiceBinding(rootDir);
        Assert.Equal("test-name-1", binding.Name);
        Assert.Equal("test-type-1", binding.Type);
        Assert.Equal("test-provider-1", binding.Provider);
        Assert.Contains("resources\\k8s\\test-name-1", binding.Path, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(binding.Secrets);
        Assert.Single(binding.Secrets);
        Assert.Equal("test-secret-value", binding.Secrets["test-secret-key"]);
    }

    [Fact]
    public void PopulatesFromFileSystem_WithHiddenFilesAndLinks_Kubernetes()
    {
        // Hidden & links
        var rootDir = GetK8SResourcesDirectory("test-k8s");
        var binding = new ServiceBindingConfigurationProvider.ServiceBinding(rootDir);
        Assert.Equal("test-k8s", binding.Name);
        Assert.Equal("test-type-1", binding.Type);
        Assert.Equal("test-provider-1", binding.Provider);
        Assert.Contains("resources\\k8s\\test-k8s", binding.Path, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(binding.Secrets);
        Assert.Single(binding.Secrets);
        Assert.Equal("test-secret-value", binding.Secrets["test-secret-key"]);
    }
    private static string GetK8SResourcesDirectory(string name)
    {
        return Path.Combine(Environment.CurrentDirectory, $"..\\..\\..\\resources\\k8s\\{name}");
    }
}

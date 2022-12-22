// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Steeltoe.Extensions.Configuration.Kubernetes.ServiceBinding.Test;

public class ServiceBindingConfigurationSourceTest
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

        builder.AddInMemoryCollection(new Dictionary<string, string>
        {
            { "steeltoe:cloud:bindings:enable", "true" }
        });

        builder.Add(source);
        IConfigurationRoot configuration = builder.Build();
        Assert.NotNull(configuration);
        Assert.NotNull(source.ParentConfiguration);
        Assert.True(source.ParentConfiguration.GetValue<bool>("steeltoe:cloud:bindings:enable"));
    }

    private static string GetK8SResourcesDirectory(string name)
    {
        return Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "resources", "k8s", $"{name}");
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.FileProviders;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.Test;

public sealed class ServiceBindingsTest
{
    [Fact]
    public void NullPath()
    {
        var b = new KubernetesServiceBindingConfigurationProvider.ServiceBindings(null);
        b.Bindings.Should().BeEmpty();
    }

    [Fact]
    public void PopulatesContent()
    {
        var path = new PhysicalFileProvider(GetK8SResourcesDirectory());
        var b = new KubernetesServiceBindingConfigurationProvider.ServiceBindings(path);
        b.Bindings.Count.Should().Be(4);
    }

    private static string GetK8SResourcesDirectory()
    {
        return Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "resources", "k8s");
    }
}

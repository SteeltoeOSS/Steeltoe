// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.Kubernetes.ServiceBindings.Test;

public sealed class ConfigurationBuilderExtensionsTest
{
    [Fact]
    public void AddKubernetesServiceBindings_RegistersProcessors()
    {
        var builder = new ConfigurationBuilder();
        builder.AddKubernetesServiceBindings();

        KubernetesServiceBindingConfigurationSource source = builder.Sources.Should().ContainSingle().Which.Should()
            .BeOfType<KubernetesServiceBindingConfigurationSource>().Subject;

        source.PostProcessors.Should().NotBeEmpty();
    }
}

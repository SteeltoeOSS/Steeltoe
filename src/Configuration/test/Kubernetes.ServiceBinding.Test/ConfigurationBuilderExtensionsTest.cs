// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.Test;

public sealed class ConfigurationBuilderExtensionsTest
{
    [Fact]
    public void AddKubernetesServiceBindings_RegistersProcessors()
    {
        var builder = new ConfigurationBuilder();
        builder.AddKubernetesServiceBindings();

        builder.Sources.Should().HaveCount(1);
        KubernetesServiceBindingConfigurationSource source = builder.Sources[0].Should().BeOfType<KubernetesServiceBindingConfigurationSource>().Subject;
        source.PostProcessors.Should().NotBeEmpty();
    }
}

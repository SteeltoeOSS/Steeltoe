// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.Test;

public class ConfigurationBuilderExtensionsTest
{
    [Fact]
    public void AddKubernetesServiceBindings_AddsSourceAndRegistersProcessors()
    {
        var builder = new ConfigurationBuilder();
        builder.AddKubernetesServiceBindings();
        Assert.Single(builder.Sources);
        Assert.IsType<ServiceBindingConfigurationSource>(builder.Sources[0]);
        var source = (ServiceBindingConfigurationSource)builder.Sources[0];
        Assert.Equal(23, source.RegisteredProcessors.Count);
    }
}

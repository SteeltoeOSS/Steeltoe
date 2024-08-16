// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding;

internal sealed class CloudFoundryServiceBindingConfigurationSource : PostProcessorConfigurationSource, IConfigurationSource
{
    private readonly IServiceBindingsReader _serviceBindingsReader;

    public CloudFoundryServiceBindingConfigurationSource(IServiceBindingsReader serviceBindingsReader)
    {
        ArgumentNullException.ThrowIfNull(serviceBindingsReader);

        _serviceBindingsReader = serviceBindingsReader;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        CaptureConfigurationBuilder(builder);
        return new CloudFoundryServiceBindingConfigurationProvider(this, _serviceBindingsReader);
    }
}

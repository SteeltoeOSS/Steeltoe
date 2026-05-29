// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.CloudFoundry.ServiceBindings;

[DebuggerDisplay("{DebuggerToString(),nq}")]
internal sealed class CloudFoundryServiceBindingConfigurationSource : PostProcessorConfigurationSource, IConfigurationSource
{
    private readonly IServiceBindingsReader _serviceBindingsReader;

    public CloudFoundryServiceBrokerTypes BrokerTypes { get; }

    public CloudFoundryServiceBindingConfigurationSource(IServiceBindingsReader serviceBindingsReader, CloudFoundryServiceBrokerTypes brokerTypes)
    {
        ArgumentNullException.ThrowIfNull(serviceBindingsReader);

        _serviceBindingsReader = serviceBindingsReader;
        BrokerTypes = brokerTypes;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        CaptureConfigurationBuilder(builder);
        return new CloudFoundryServiceBindingConfigurationProvider(this, _serviceBindingsReader);
    }

    private string DebuggerToString()
    {
        return $"{GetType().FullName} ({BrokerTypes})";
    }
}

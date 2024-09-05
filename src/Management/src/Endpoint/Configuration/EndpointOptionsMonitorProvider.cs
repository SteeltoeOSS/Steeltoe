// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Configuration;

/// <summary>
/// Provides access to the latest value for the specified endpoint options type.
/// </summary>
/// <typeparam name="T">
/// The <see cref="EndpointOptions" /> type.
/// </typeparam>
internal sealed class EndpointOptionsMonitorProvider<T> : IEndpointOptionsMonitorProvider
    where T : EndpointOptions
{
    private readonly IOptionsMonitor<T> _optionsMonitor;

    public EndpointOptionsMonitorProvider(IOptionsMonitor<T> optionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        _optionsMonitor = optionsMonitor;
    }

    public EndpointOptions Get()
    {
        return _optionsMonitor.CurrentValue;
    }
}

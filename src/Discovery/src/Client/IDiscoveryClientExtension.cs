// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Discovery;
using Steeltoe.Connectors.Services;

namespace Steeltoe.Discovery.Client;

public interface IDiscoveryClientExtension
{
    /// <summary>
    /// Indicates whether expected configuration keys for the associated <see cref="IDiscoveryClient" /> are present.
    /// </summary>
    /// <param name="configuration">
    /// The <see cref="IConfiguration" /> to read application settings from.
    /// </param>
    /// <param name="serviceInfo">
    /// Optional service binding credentials.
    /// </param>
    bool IsConfigured(IConfiguration configuration, IServiceInfo? serviceInfo);

    /// <summary>
    /// Adds services required by the associated <see cref="IDiscoveryClient" /> to the service collection for activation later.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    void ApplyServices(IServiceCollection services);
}

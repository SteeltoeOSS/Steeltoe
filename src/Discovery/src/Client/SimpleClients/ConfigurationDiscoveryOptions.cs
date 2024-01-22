// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common.Discovery;

namespace Steeltoe.Discovery.Client.SimpleClients;

/// <summary>
/// Provides access to <see cref="IServiceInstance" />s stored in app configuration. used by <see cref="ConfigurationDiscoveryClient" />.
/// </summary>
public sealed class ConfigurationDiscoveryOptions
{
    public IList<ConfigurationServiceInstance> Services { get; } = new List<ConfigurationServiceInstance>();
}

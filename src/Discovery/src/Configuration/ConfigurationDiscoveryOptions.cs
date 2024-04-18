// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Discovery;

namespace Steeltoe.Discovery.Configuration;

/// <summary>
/// Provides access to <see cref="IServiceInstance" />s stored in app configuration. Used by <see cref="ConfigurationDiscoveryClient" />.
/// </summary>
public sealed class ConfigurationDiscoveryOptions
{
    internal const string ConfigurationPrefix = "discovery";

    public IList<ConfigurationServiceInstance> Services { get; } = new List<ConfigurationServiceInstance>();
}

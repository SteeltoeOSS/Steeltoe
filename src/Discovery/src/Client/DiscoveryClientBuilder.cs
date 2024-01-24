// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Discovery;

namespace Steeltoe.Discovery.Client;

public sealed class DiscoveryClientBuilder
{
    /// <summary>
    /// Gets the list of extensions to use when configuring an <see cref="IDiscoveryClient" />.
    /// </summary>
    /// <remarks>
    /// An application should generally only use one extension.
    /// </remarks>
    public IList<IDiscoveryClientExtension> Extensions { get; } = new List<IDiscoveryClientExtension>();
}

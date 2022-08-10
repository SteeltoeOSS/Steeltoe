// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Attributes;
using Steeltoe.Discovery.Client;

namespace Steeltoe.Discovery;

/// <summary>
/// Identify assemblies containing ServiceInfoCreators.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class DiscoveryClientAssemblyAttribute : AssemblyContainsTypeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoveryClientAssemblyAttribute" /> class. Used to identify assemblies that contain a discovery client.
    /// </summary>
    /// <param name="discoveryClientExtensionType">
    /// The <see cref="IDiscoveryClientExtension" />.
    /// </param>
    public DiscoveryClientAssemblyAttribute(Type discoveryClientExtensionType)
        : base(discoveryClientExtensionType)
    {
    }
}

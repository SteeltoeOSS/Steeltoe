// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration;

/// <summary>
/// Represents a configuration source that is composed of multiple other configuration sources.
/// </summary>
public interface ICompositeConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// Gets the sources that are owned by this composite source.
    /// </summary>
    IList<IConfigurationSource> Sources { get; }
}

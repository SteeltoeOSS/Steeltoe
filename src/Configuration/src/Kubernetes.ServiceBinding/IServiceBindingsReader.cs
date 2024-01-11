// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.FileProviders;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding;

/// <summary>
/// Provides a method to read the Kubernetes secret files on disk.
/// </summary>
public interface IServiceBindingsReader
{
    /// <summary>
    /// Returns the root directory to read secret files from.
    /// </summary>
    /// <returns>
    /// The root directory, or <c>null</c> when unavailable.
    /// </returns>
    IFileProvider? GetRootDirectory();
}

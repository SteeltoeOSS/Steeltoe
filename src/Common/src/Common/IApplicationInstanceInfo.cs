// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common;

/// <summary>
/// Provides information about the currently running application instance.
/// </summary>
public interface IApplicationInstanceInfo
{
    /// <summary>
    /// Gets the name of this application.
    /// </summary>
    string? ApplicationName { get; }
}

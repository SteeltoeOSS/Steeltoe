// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Lifecycle;

/// <summary>
/// An interface for objects that participate in a phased lifecycle.
/// </summary>
public interface IPhased
{
    /// <summary>
    /// Gets the phase of this object
    /// </summary>
    int Phase { get; }
}

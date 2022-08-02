// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Discovery.Consul.Discovery;

/// <summary>
/// Scheduler used for scheduling heartbeats to the Consul server.
/// </summary>
public interface IScheduler : IDisposable
{
    /// <summary>
    /// Adds an instances id to be checked.
    /// </summary>
    /// <param name="instanceId">
    /// the instance id.
    /// </param>
    void Add(string instanceId);

    /// <summary>
    /// Remove an instance id from checking.
    /// </summary>
    /// <param name="instanceId">
    /// the instance id.
    /// </param>
    void Remove(string instanceId);
}

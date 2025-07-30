// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Integration.Channel;

/// <summary>
/// Interface implemented by task scheduler based channels
/// </summary>
[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public interface ITaskSchedulerChannelInterceptorAware : IChannelInterceptorAware
{
    /// <summary>
    /// Gets a value indicating whether there are any task scheduler interceptors on the channel
    /// </summary>
    bool HasTaskSchedulerInterceptors { get; }
}
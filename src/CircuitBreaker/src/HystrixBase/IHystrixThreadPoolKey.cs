// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix;

/// <summary>
/// A key to represent a <seealso cref="IHystrixThreadPool" /> for monitoring, metrics publishing, caching and other such uses.
/// </summary>
public interface IHystrixThreadPoolKey : IHystrixKey
{
}

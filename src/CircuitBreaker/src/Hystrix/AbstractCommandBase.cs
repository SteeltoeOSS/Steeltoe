// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix;

public abstract class AbstractCommandBase
{
    // we can return a static version since it's immutable
    internal static readonly ExecutionResult Empty = ExecutionResult.From();

    protected static readonly ConcurrentDictionary<string, SemaphoreSlim> ExecutionSemaphorePerCircuit = new();
    protected static readonly ConcurrentDictionary<string, SemaphoreSlim> FallbackSemaphorePerCircuit = new();
}

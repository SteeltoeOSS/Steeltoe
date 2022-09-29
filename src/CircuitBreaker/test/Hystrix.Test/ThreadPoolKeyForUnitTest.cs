// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

public static class ThreadPoolKeyForUnitTest
{
    public static IHystrixThreadPoolKey ThreadPoolOne { get; } = new HystrixThreadPoolKeyDefault("THREAD_POOL_ONE");
    public static IHystrixThreadPoolKey ThreadPoolTwo { get; } = new HystrixThreadPoolKeyDefault("THREAD_POOL_TWO");
}

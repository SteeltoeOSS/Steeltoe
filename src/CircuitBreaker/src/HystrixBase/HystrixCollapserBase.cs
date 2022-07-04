// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix;

public abstract class HystrixCollapserBase
{
    // this is a micro-optimization but saves about 1-2microseconds (on 2011 MacBook Pro)
    // on the repetitive string processing that will occur on the same classes over and over again
    protected static readonly ConcurrentDictionary<Type, string> DefaultNameCache = new ();
}

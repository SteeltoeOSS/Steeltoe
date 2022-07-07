// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.CircuitBreaker;

public class NoOpCircuitBreaker : ICircuitBreaker
{
    public void MarkSuccess()
    {
        // Don't do anything here (no-operation)
    }

    public bool AllowRequest => true;

    public bool IsOpen => false;
}

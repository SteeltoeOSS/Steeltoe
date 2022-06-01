// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

public class TestCircuitBreaker : ICircuitBreaker
{
    public readonly HystrixCommandMetrics Metrics;
    private bool forceShortCircuit;

    public TestCircuitBreaker()
    {
        Metrics = HystrixCircuitBreakerTest.GetMetrics(HystrixCommandOptionsTest.GetUnitTestOptions());
        forceShortCircuit = false;
    }

    public TestCircuitBreaker(IHystrixCommandKey commandKey)
    {
        Metrics = HystrixCircuitBreakerTest.GetMetrics(commandKey, HystrixCommandOptionsTest.GetUnitTestOptions());
        forceShortCircuit = false;
    }

    public TestCircuitBreaker SetForceShortCircuit(bool value)
    {
        forceShortCircuit = value;
        return this;
    }

    public bool IsOpen
    {
        get
        {
            // output.WriteLine("metrics : " + metrics.CommandKey.Name + " : " + metrics.Healthcounts);
            if (forceShortCircuit)
            {
                return true;
            }
            else
            {
                return Metrics.Healthcounts.ErrorCount >= 3;
            }
        }
    }

    public void MarkSuccess()
    {
        // we don't need to do anything since we're going to permanently trip the circuit
    }

    public bool AllowRequest
    {
        get { return !IsOpen; }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

public class TestCircuitBreaker : ICircuitBreaker
{
    public readonly HystrixCommandMetrics Metrics;
    private bool _forceShortCircuit;

    public bool IsOpen
    {
        get
        {
            // output.WriteLine("metrics : " + metrics.CommandKey.Name + " : " + metrics.HealthCounts);
            if (_forceShortCircuit)
            {
                return true;
            }

            return Metrics.HealthCounts.ErrorCount >= 3;
        }
    }

    public bool AllowRequest => !IsOpen;

    public TestCircuitBreaker()
    {
        Metrics = HystrixCircuitBreakerTest.GetMetrics(HystrixCommandOptionsTest.GetUnitTestOptions());
        _forceShortCircuit = false;
    }

    public TestCircuitBreaker(IHystrixCommandKey commandKey)
    {
        Metrics = HystrixCircuitBreakerTest.GetMetrics(commandKey, HystrixCommandOptionsTest.GetUnitTestOptions());
        _forceShortCircuit = false;
    }

    public TestCircuitBreaker SetForceShortCircuit(bool value)
    {
        _forceShortCircuit = value;
        return this;
    }

    public void MarkSuccess()
    {
        // we don't need to do anything since we're going to permanently trip the circuit
    }
}

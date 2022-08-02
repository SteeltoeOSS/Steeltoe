// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class SuccessfulCacheableCommandViaSemaphore : TestHystrixCommand<string>
{
    private readonly bool _cacheEnabled;
    private readonly string _value;
    public volatile bool Executed;

    protected override string CacheKey
    {
        get
        {
            if (_cacheEnabled)
            {
                return _value;
            }

            return null;
        }
    }

    public bool IsCommandRunningInThread => CommandOptions.ExecutionIsolationStrategy.Equals(ExecutionIsolationStrategy.Thread);

    public SuccessfulCacheableCommandViaSemaphore(TestCircuitBreaker circuitBreaker, bool cacheEnabled, string value)
        : base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.Metrics)
            .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions())))
    {
        _value = value;
        _cacheEnabled = cacheEnabled;
    }

    protected override string Run()
    {
        Executed = true;

        Output?.WriteLine("successfully executed");
        return _value;
    }

    private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions)
    {
        hystrixCommandOptions.ExecutionIsolationStrategy = ExecutionIsolationStrategy.Semaphore;
        hystrixCommandOptions.CircuitBreakerEnabled = false;
        return hystrixCommandOptions;
    }
}

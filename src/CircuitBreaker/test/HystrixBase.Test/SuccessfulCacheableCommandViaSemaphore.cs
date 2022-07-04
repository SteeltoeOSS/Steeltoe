// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class SuccessfulCacheableCommandViaSemaphore : TestHystrixCommand<string>
{
    public volatile bool Executed;
    private readonly bool _cacheEnabled;
    private readonly string _value;

    public SuccessfulCacheableCommandViaSemaphore(TestCircuitBreaker circuitBreaker, bool cacheEnabled, string value)
        : base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.Metrics)
            .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions())))
    {
        _value = value;
        _cacheEnabled = cacheEnabled;
    }

    public bool IsCommandRunningInThread
    {
        get { return CommandOptions.ExecutionIsolationStrategy.Equals(ExecutionIsolationStrategy.Thread); }
    }

    protected override string Run()
    {
        Executed = true;

        Output?.WriteLine("successfully executed");
        return _value;
    }

    protected override string CacheKey
    {
        get
        {
            if (_cacheEnabled)
            {
                return _value;
            }
            else
            {
                return null;
            }
        }
    }

    private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions)
    {
        hystrixCommandOptions.ExecutionIsolationStrategy = ExecutionIsolationStrategy.Semaphore;
        hystrixCommandOptions.CircuitBreakerEnabled = false;
        return hystrixCommandOptions;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class SuccessfulCacheableCommand<T> : TestHystrixCommand<T>
{
    public volatile bool Executed;
    private readonly bool _cacheEnabled;
    private readonly T _value;

    public SuccessfulCacheableCommand(TestCircuitBreaker circuitBreaker, bool cacheEnabled, T value)
        : base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.Metrics))
    {
        _value = value;
        _cacheEnabled = cacheEnabled;
    }

    protected override T Run()
    {
        Executed = true;

        Output?.WriteLine("successfully executed");
        return _value;
    }

    public bool IsCommandRunningInThread
    {
        get { return CommandOptions.ExecutionIsolationStrategy.Equals(ExecutionIsolationStrategy.Thread); }
    }

    protected override string CacheKey
    {
        get
        {
            if (_cacheEnabled)
            {
                return _value.ToString();
            }
            else
            {
                return null;
            }
        }
    }
}

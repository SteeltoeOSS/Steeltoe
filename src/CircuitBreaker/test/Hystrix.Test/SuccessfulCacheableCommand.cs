// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class SuccessfulCacheableCommand<T> : TestHystrixCommand<T>
{
    private readonly bool _cacheEnabled;
    private readonly T _value;
    public volatile bool Executed;

    protected override string CacheKey
    {
        get
        {
            if (_cacheEnabled)
            {
                return _value.ToString();
            }

            return null;
        }
    }

    public bool IsCommandRunningInThread => CommandOptions.ExecutionIsolationStrategy.Equals(ExecutionIsolationStrategy.Thread);

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
}

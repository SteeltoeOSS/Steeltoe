// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class SlowCacheableCommand : TestHystrixCommand<string>
{
    public volatile bool Executed;
    private readonly string _value;
    private readonly int _duration;

    public SlowCacheableCommand(TestCircuitBreaker circuitBreaker, string value, int duration)
        : base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.Metrics))
    {
        _value = value;
        _duration = duration;
    }

    protected override string Run()
    {
        Executed = true;
        Time.Wait(_duration);

        Output?.WriteLine("successfully executed");
        return _value;
    }

    protected override string CacheKey
    {
        get { return _value; }
    }
}

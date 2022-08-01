// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class TestCommandRejection : TestHystrixCommand<bool>
{
    public const int FallbackNotImplemented = 1;
    public const int FallbackSuccess = 2;
    public const int FallbackFailure = 3;

    private readonly int _fallbackBehavior;

    private readonly int _sleepTime;
    private readonly ITestOutputHelper _outputHelper;

    public TestCommandRejection(ITestOutputHelper outputHelper, IHystrixCommandKey key, TestCircuitBreaker circuitBreaker, IHystrixThreadPool threadPool, int sleepTime, int timeout, int fallbackBehavior)
        : this(key, circuitBreaker, threadPool, sleepTime, timeout, fallbackBehavior)
    {
        _outputHelper = outputHelper;
    }

    public TestCommandRejection(IHystrixCommandKey key, TestCircuitBreaker circuitBreaker, IHystrixThreadPool threadPool, int sleepTime, int timeout, int fallbackBehavior)
        : base(TestPropsBuilder()
            .SetCommandKey(key)
            .SetThreadPool(threadPool)
            .SetCircuitBreaker(circuitBreaker)
            .SetMetrics(circuitBreaker.Metrics)
            .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions(), timeout)))
    {
        _fallbackBehavior = fallbackBehavior;
        _sleepTime = sleepTime;
    }

    protected override bool Run()
    {
        var start = DateTime.Now.Ticks / 10000;
        _outputHelper?.WriteLine(">>> TestCommandRejection running " + _sleepTime);
        try
        {
            Time.WaitUntil(() => Token.IsCancellationRequested, _sleepTime);
            Token.ThrowIfCancellationRequested();
            _outputHelper?.WriteLine(">>> TestCommandRejection finished " + (Time.CurrentTimeMillis - start));
        }
        catch (Exception e)
        {
            _outputHelper?.WriteLine(">>> TestCommandRejection finished " + (Time.CurrentTimeMillis - start));
            _outputHelper?.WriteLine(">>> TestCommandRejection exception: " + e);
        }

        return true;
    }

    protected override bool RunFallback()
    {
        if (_fallbackBehavior == FallbackSuccess)
        {
            return false;
        }
        else if (_fallbackBehavior == FallbackFailure)
        {
            throw new Exception("failed on fallback");
        }
        else
        {
            // FALLBACK_NOT_IMPLEMENTED
            return base.RunFallback();
        }
    }

    private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions, int timeout)
    {
        hystrixCommandOptions.ExecutionTimeoutInMilliseconds = timeout;
        return hystrixCommandOptions;
    }
}

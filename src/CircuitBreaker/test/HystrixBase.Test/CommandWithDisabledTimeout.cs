// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class CommandWithDisabledTimeout : TestHystrixCommand<bool>
{
    private readonly int _latency;

    public CommandWithDisabledTimeout(int timeout, int latency)
        : base(TestPropsBuilder().SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions(), timeout)))
    {
        _latency = latency;
    }

    protected override bool Run()
    {
        try
        {
            Time.Wait(_latency);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    protected override bool RunFallback()
    {
        return false;
    }

    private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions, int timeout)
    {
        hystrixCommandOptions.ExecutionTimeoutInMilliseconds = timeout;
        hystrixCommandOptions.ExecutionTimeoutEnabled = false;
        return hystrixCommandOptions;
    }
}

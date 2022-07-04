// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

public class HystrixSubclassCommandTest : HystrixTestBase
{
    private static readonly IHystrixCommandGroupKey GroupKey = HystrixCommandGroupKeyDefault.AsKey("GROUP");

    private readonly ITestOutputHelper _output;

    public HystrixSubclassCommandTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TestFallback()
    {
        HystrixCommand<int> superCmd = new SuperCommand("cache", false);
        Assert.Equal(2, superCmd.Execute());

        HystrixCommand<int> subNoOverridesCmd = new SubCommandNoOverride("cache", false);
        Assert.Equal(2, subNoOverridesCmd.Execute());

        HystrixCommand<int> subOverriddenFallbackCmd = new SubCommandOverrideFallback("cache", false);
        Assert.Equal(3, subOverriddenFallbackCmd.Execute());
    }

    [Fact]
    public void TestRequestCacheSuperClass()
    {
        HystrixCommand<int> superCmd1 = new SuperCommand("cache", true);
        Assert.Equal(1, superCmd1.Execute());
        HystrixCommand<int> superCmd2 = new SuperCommand("cache", true);
        Assert.Equal(1, superCmd2.Execute());
        HystrixCommand<int> superCmd3 = new SuperCommand("no-cache", true);
        Assert.Equal(1, superCmd3.Execute());
        _output.WriteLine("REQ LOG : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        var reqLog = HystrixRequestLog.CurrentRequestLog;
        Assert.Equal(3, reqLog.AllExecutedCommands.Count);
        var infos = new List<IHystrixInvokableInfo>(reqLog.AllExecutedCommands);
        var info1 = infos[0];
        Assert.Equal("SuperCommand", info1.CommandKey.Name);
        Assert.Single(info1.ExecutionEvents);
        var info2 = infos[1];
        Assert.Equal("SuperCommand", info2.CommandKey.Name);
        Assert.Equal(2, info2.ExecutionEvents.Count);
        Assert.Equal(HystrixEventType.ResponseFromCache, info2.ExecutionEvents[1]);
        var info3 = infos[2];
        Assert.Equal("SuperCommand", info3.CommandKey.Name);
        Assert.Single(info3.ExecutionEvents);
    }

    [Fact]
    public void TestRequestCacheSubclassNoOverrides()
    {
        HystrixCommand<int> subCmd1 = new SubCommandNoOverride("cache", true);
        Assert.Equal(1, subCmd1.Execute());
        HystrixCommand<int> subCmd2 = new SubCommandNoOverride("cache", true);
        Assert.Equal(1, subCmd2.Execute());
        HystrixCommand<int> subCmd3 = new SubCommandNoOverride("no-cache", true);
        Assert.Equal(1, subCmd3.Execute());
        _output.WriteLine("REQ LOG : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        var reqLog = HystrixRequestLog.CurrentRequestLog;
        Assert.Equal(3, reqLog.AllExecutedCommands.Count);
        var infos = new List<IHystrixInvokableInfo>(reqLog.AllExecutedCommands);

        var info1 = infos[0];
        Assert.Equal("SubCommandNoOverride", info1.CommandKey.Name);
        Assert.Single(info1.ExecutionEvents);
        var info2 = infos[1];
        Assert.Equal("SubCommandNoOverride", info2.CommandKey.Name);
        Assert.Equal(2, info2.ExecutionEvents.Count);
        Assert.Equal(HystrixEventType.ResponseFromCache, info2.ExecutionEvents[1]);
        var info3 = infos[2];
        Assert.Equal("SubCommandNoOverride", info3.CommandKey.Name);
        Assert.Single(info3.ExecutionEvents);
    }

    [Fact]
    public void TestRequestLogSuperClass()
    {
        HystrixCommand<int> superCmd = new SuperCommand("cache", true);
        Assert.Equal(1, superCmd.Execute());
        _output.WriteLine("REQ LOG : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        var reqLog = HystrixRequestLog.CurrentRequestLog;
        Assert.Equal(1, reqLog.AllExecutedCommands.Count);
        var info = reqLog.AllExecutedCommands.ToList()[0];
        Assert.Equal("SuperCommand", info.CommandKey.Name);
    }

    [Fact]
    public void TestRequestLogSubClassNoOverrides()
    {
        HystrixCommand<int> subCmd = new SubCommandNoOverride("cache", true);
        Assert.Equal(1, subCmd.Execute());
        _output.WriteLine("REQ LOG : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        var reqLog = HystrixRequestLog.CurrentRequestLog;
        Assert.Equal(1, reqLog.AllExecutedCommands.Count);
        var info = reqLog.AllExecutedCommands.ToList()[0];
        Assert.Equal("SubCommandNoOverride", info.CommandKey.Name);
    }

    private class SuperCommand : HystrixCommand<int>
    {
        private readonly bool _shouldSucceed;

        public SuperCommand(string uniqueArg, bool shouldSucceed)
            : base(GroupKey)
        {
            CacheKey = uniqueArg;
            _shouldSucceed = shouldSucceed;
            IsFallbackUserDefined = true;
        }

        protected override int Run()
        {
            if (_shouldSucceed)
            {
                return 1;
            }
            else
            {
                throw new Exception("unit test failure");
            }
        }

        protected override int RunFallback()
        {
            return 2;
        }

        protected override string CacheKey { get; }
    }

    private sealed class SubCommandNoOverride : SuperCommand
    {
        public SubCommandNoOverride(string uniqueArg, bool shouldSucceed)
            : base(uniqueArg, shouldSucceed)
        {
        }
    }

    private sealed class SubCommandOverrideFallback : SuperCommand
    {
        public SubCommandOverrideFallback(string uniqueArg, bool shouldSucceed)
            : base(uniqueArg, shouldSucceed)
        {
            IsFallbackUserDefined = true;
        }

        protected override int RunFallback()
        {
            return 3;
        }
    }
}

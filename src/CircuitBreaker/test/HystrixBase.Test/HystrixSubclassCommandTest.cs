﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public class HystrixSubclassCommandTest : HystrixTestBase
    {
        private static readonly IHystrixCommandGroupKey GroupKey = HystrixCommandGroupKeyDefault.AsKey("GROUP");

        private ITestOutputHelper output;

        public HystrixSubclassCommandTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;
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
            output.WriteLine("REQ LOG : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            HystrixRequestLog reqLog = HystrixRequestLog.CurrentRequestLog;
            Assert.Equal(3, reqLog.AllExecutedCommands.Count);
            List<IHystrixInvokableInfo> infos = new List<IHystrixInvokableInfo>(reqLog.AllExecutedCommands);
            IHystrixInvokableInfo info1 = infos[0];
            Assert.Equal("SuperCommand", info1.CommandKey.Name);
            Assert.Single(info1.ExecutionEvents);
            IHystrixInvokableInfo info2 = infos[1];
            Assert.Equal("SuperCommand", info2.CommandKey.Name);
            Assert.Equal(2, info2.ExecutionEvents.Count);
            Assert.Equal(HystrixEventType.RESPONSE_FROM_CACHE, info2.ExecutionEvents[1]);
            IHystrixInvokableInfo info3 = infos[2];
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
            output.WriteLine("REQ LOG : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            HystrixRequestLog reqLog = HystrixRequestLog.CurrentRequestLog;
            Assert.Equal(3, reqLog.AllExecutedCommands.Count);
            List<IHystrixInvokableInfo> infos = new List<IHystrixInvokableInfo>(reqLog.AllExecutedCommands);

            IHystrixInvokableInfo info1 = infos[0];
            Assert.Equal("SubCommandNoOverride", info1.CommandKey.Name);
            Assert.Single(info1.ExecutionEvents);
            IHystrixInvokableInfo info2 = infos[1];
            Assert.Equal("SubCommandNoOverride", info2.CommandKey.Name);
            Assert.Equal(2, info2.ExecutionEvents.Count);
            Assert.Equal(HystrixEventType.RESPONSE_FROM_CACHE, info2.ExecutionEvents[1]);
            IHystrixInvokableInfo info3 = infos[2];
            Assert.Equal("SubCommandNoOverride", info3.CommandKey.Name);
            Assert.Single(info3.ExecutionEvents);
        }

        [Fact]
        public void TestRequestLogSuperClass()
        {
            HystrixCommand<int> superCmd = new SuperCommand("cache", true);
            Assert.Equal(1, superCmd.Execute());
            output.WriteLine("REQ LOG : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            HystrixRequestLog reqLog = HystrixRequestLog.CurrentRequestLog;
            Assert.Equal(1, reqLog.AllExecutedCommands.Count);
            IHystrixInvokableInfo info = reqLog.AllExecutedCommands.ToList()[0];
            Assert.Equal("SuperCommand", info.CommandKey.Name);
        }

        [Fact]
        public void TestRequestLogSubClassNoOverrides()
        {
            HystrixCommand<int> subCmd = new SubCommandNoOverride("cache", true);
            Assert.Equal(1, subCmd.Execute());
            output.WriteLine("REQ LOG : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            HystrixRequestLog reqLog = HystrixRequestLog.CurrentRequestLog;
            Assert.Equal(1, reqLog.AllExecutedCommands.Count);
            IHystrixInvokableInfo info = reqLog.AllExecutedCommands.ToList()[0];
            Assert.Equal("SubCommandNoOverride", info.CommandKey.Name);
        }

        private class SuperCommand : HystrixCommand<int>
        {
            private readonly string uniqueArg;
            private readonly bool shouldSucceed;

            public SuperCommand(string uniqueArg, bool shouldSucceed)
            : base(GroupKey)
            {
                this.uniqueArg = uniqueArg;
                this.shouldSucceed = shouldSucceed;
                this.IsFallbackUserDefined = true;
            }

            protected override int Run()
            {
                if (shouldSucceed)
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

            protected override string CacheKey
            {
                get { return uniqueArg; }
            }
        }

        private class SubCommandNoOverride : SuperCommand
        {
            public SubCommandNoOverride(string uniqueArg, bool shouldSucceed)
                : base(uniqueArg, shouldSucceed)
            {
            }
        }

        private class SubCommandOverrideFallback : SuperCommand
        {
            public SubCommandOverrideFallback(string uniqueArg, bool shouldSucceed)
                : base(uniqueArg, shouldSucceed)
            {
                this.IsFallbackUserDefined = true;
            }

            protected override int RunFallback()
            {
                return 3;
            }
        }
    }
}

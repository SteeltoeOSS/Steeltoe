// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Common.Lifecycle;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Common.Test.Lifecycle
{
    public class DefaultLifecycleProcessorTest
    {
        [Fact]
        public async Task SingleSmartLifecycleAutoStartup()
        {
            var startedBeans = new ConcurrentQueue<ILifecycle>();
            var bean = TestSmartLifecycleBean.ForStartupTests(1, startedBeans);
            bean.IsAutoStartup = true;
            var processor = new DefaultLifecycleProcessor(new List<ILifecycle>() { bean }, new List<ISmartLifecycle>());
            Assert.False(bean.IsRunning);
            await processor.OnRefresh();
            Assert.True(bean.IsRunning);
            await processor.Stop();
            Assert.False(bean.IsRunning);
            Assert.Single(startedBeans);
        }

        [Fact]
        public async Task SingleSmartLifecycleWithoutAutoStartup()
        {
            var startedBeans = new ConcurrentQueue<ILifecycle>();
            var bean = TestSmartLifecycleBean.ForStartupTests(1, startedBeans);
            bean.IsAutoStartup = false;
            var processor = new DefaultLifecycleProcessor(new List<ILifecycle>() { bean }, new List<ISmartLifecycle>());
            Assert.False(bean.IsRunning);
            await processor.OnRefresh();
            Assert.False(bean.IsRunning);
            Assert.Empty(startedBeans);
            await processor.Start();
            Assert.True(bean.IsRunning);
            Assert.Single(startedBeans);
            await processor.Stop();
        }

        [Fact]
        public async Task SmartLifecycleGroupStartup()
        {
            var startedBeans = new ConcurrentQueue<ILifecycle>();
            var beanMin = TestSmartLifecycleBean.ForStartupTests(int.MinValue, startedBeans);
            var bean1 = TestSmartLifecycleBean.ForStartupTests(1, startedBeans);
            var bean2 = TestSmartLifecycleBean.ForStartupTests(2, startedBeans);
            var bean3 = TestSmartLifecycleBean.ForStartupTests(3, startedBeans);
            var beanMax = TestSmartLifecycleBean.ForStartupTests(int.MaxValue, startedBeans);
            var processor = new DefaultLifecycleProcessor(new List<ILifecycle>() { bean3, beanMin, bean2, beanMax, bean1 }, new List<ISmartLifecycle>());

            Assert.False(beanMin.IsRunning);
            Assert.False(bean1.IsRunning);
            Assert.False(bean2.IsRunning);
            Assert.False(bean3.IsRunning);
            Assert.False(beanMax.IsRunning);

            await processor.OnRefresh();

            Assert.True(beanMin.IsRunning);
            Assert.True(bean1.IsRunning);
            Assert.True(bean2.IsRunning);
            Assert.True(bean3.IsRunning);
            Assert.True(beanMax.IsRunning);

            await processor.Stop();

            Assert.Equal(5, startedBeans.Count);
            var started = startedBeans.ToArray();
            Assert.Equal(int.MinValue, GetPhase(started[0]));
            Assert.Equal(1, GetPhase(started[1]));
            Assert.Equal(2, GetPhase(started[2]));
            Assert.Equal(3, GetPhase(started[3]));
            Assert.Equal(int.MaxValue, GetPhase(started[4]));
        }

        [Fact]
        public async Task RefreshThenStartWithMixedBeans()
        {
            var startedBeans = new ConcurrentQueue<ILifecycle>();
            var simpleBean1 = TestLifecycleBean.ForStartupTests(startedBeans);
            var simpleBean2 = TestLifecycleBean.ForStartupTests(startedBeans);
            var smartBean1 = TestSmartLifecycleBean.ForStartupTests(5, startedBeans);
            var smartBean2 = TestSmartLifecycleBean.ForStartupTests(-3, startedBeans);
            var processor = new DefaultLifecycleProcessor(new List<ILifecycle>() { simpleBean1, simpleBean2, smartBean1, smartBean2 }, new List<ISmartLifecycle>());

            Assert.False(simpleBean1.IsRunning);
            Assert.False(simpleBean2.IsRunning);
            Assert.False(smartBean1.IsRunning);
            Assert.False(smartBean2.IsRunning);

            await processor.OnRefresh();

            Assert.False(simpleBean1.IsRunning);
            Assert.False(simpleBean2.IsRunning);
            Assert.True(smartBean1.IsRunning);
            Assert.True(smartBean2.IsRunning);

            Assert.Equal(2, startedBeans.Count);
            var started = startedBeans.ToArray();
            Assert.Equal(-3, GetPhase(started[0]));
            Assert.Equal(5, GetPhase(started[1]));

            await processor.Start();

            Assert.True(simpleBean1.IsRunning);
            Assert.True(simpleBean2.IsRunning);
            Assert.True(smartBean1.IsRunning);
            Assert.True(smartBean2.IsRunning);

            Assert.Equal(4, startedBeans.Count);
            started = startedBeans.ToArray();
            Assert.Equal(0, GetPhase(started[2]));
            Assert.Equal(0, GetPhase(started[3]));
        }

        [Fact]
        public async Task RefreshThenStopAndRestartWithMixedBeans()
        {
            var startedBeans = new ConcurrentQueue<ILifecycle>();
            var simpleBean1 = TestLifecycleBean.ForStartupTests(startedBeans);
            var simpleBean2 = TestLifecycleBean.ForStartupTests(startedBeans);
            var smartBean1 = TestSmartLifecycleBean.ForStartupTests(5, startedBeans);
            var smartBean2 = TestSmartLifecycleBean.ForStartupTests(-3, startedBeans);
            var processor = new DefaultLifecycleProcessor(new List<ILifecycle>() { simpleBean1, simpleBean2, smartBean1, smartBean2 }, new List<ISmartLifecycle>());

            Assert.False(simpleBean1.IsRunning);
            Assert.False(simpleBean2.IsRunning);
            Assert.False(smartBean1.IsRunning);
            Assert.False(smartBean2.IsRunning);

            await processor.OnRefresh();

            Assert.False(simpleBean1.IsRunning);
            Assert.False(simpleBean2.IsRunning);
            Assert.True(smartBean1.IsRunning);
            Assert.True(smartBean2.IsRunning);

            Assert.Equal(2, startedBeans.Count);
            var started = startedBeans.ToArray();
            Assert.Equal(-3, GetPhase(started[0]));
            Assert.Equal(5, GetPhase(started[1]));

            await processor.Stop();

            Assert.False(simpleBean1.IsRunning);
            Assert.False(simpleBean2.IsRunning);
            Assert.False(smartBean1.IsRunning);
            Assert.False(smartBean2.IsRunning);

            await processor.Start();

            Assert.True(simpleBean1.IsRunning);
            Assert.True(simpleBean2.IsRunning);
            Assert.True(smartBean1.IsRunning);
            Assert.True(smartBean2.IsRunning);

            Assert.Equal(6, startedBeans.Count);
            started = startedBeans.ToArray();
            Assert.Equal(-3, GetPhase(started[2]));
            Assert.Equal(0, GetPhase(started[3]));
            Assert.Equal(0, GetPhase(started[4]));
            Assert.Equal(5, GetPhase(started[5]));
        }

        [Fact]
        public async Task SmartLifecycleGroupShutdown()
        {
            var stoppedBeans = new ConcurrentQueue<ILifecycle>();

            var bean1 = TestSmartLifecycleBean.ForShutdownTests(1, 300, stoppedBeans);
            var bean2 = TestSmartLifecycleBean.ForShutdownTests(3, 100, stoppedBeans);
            var bean3 = TestSmartLifecycleBean.ForShutdownTests(1, 600, stoppedBeans);
            var bean4 = TestSmartLifecycleBean.ForShutdownTests(2, 400, stoppedBeans);
            var bean5 = TestSmartLifecycleBean.ForShutdownTests(2, 700, stoppedBeans);
            var bean6 = TestSmartLifecycleBean.ForShutdownTests(int.MaxValue, 200, stoppedBeans);
            var bean7 = TestSmartLifecycleBean.ForShutdownTests(3, 200, stoppedBeans);

            var processor = new DefaultLifecycleProcessor(new List<ILifecycle>() { bean1, bean2, bean3, bean4, bean5, bean6, bean7 }, new List<ISmartLifecycle>());
            await processor.OnRefresh();
            await processor.Stop();
            var stopped = stoppedBeans.ToArray();
            Assert.Equal(int.MaxValue, GetPhase(stopped[0]));
            Assert.Equal(3, GetPhase(stopped[1]));
            Assert.Equal(3, GetPhase(stopped[2]));
            Assert.Equal(2, GetPhase(stopped[3]));
            Assert.Equal(2, GetPhase(stopped[4]));
            Assert.Equal(1, GetPhase(stopped[5]));
            Assert.Equal(1, GetPhase(stopped[6]));
        }

        [Fact]
        public async Task SingleSmartLifecycleShutdown()
        {
            var stoppedBeans = new ConcurrentQueue<ILifecycle>();
            var bean = TestSmartLifecycleBean.ForShutdownTests(99, 300, stoppedBeans);

            var processor = new DefaultLifecycleProcessor(new List<ILifecycle>() { bean }, new List<ISmartLifecycle>());
            await processor.OnRefresh();
            Assert.True(bean.IsRunning);
            await processor.Stop();
            var stopped = stoppedBeans.ToArray();
            Assert.Same(bean, stopped[0]);
        }

        [Fact]
        public async Task SingleLifecycleShutdown()
        {
            var stoppedBeans = new ConcurrentQueue<ILifecycle>();
            ILifecycle bean = new TestLifecycleBean(null, stoppedBeans);

            var processor = new DefaultLifecycleProcessor(new List<ILifecycle>() { bean }, new List<ISmartLifecycle>());
            Assert.False(bean.IsRunning);
            await processor.OnRefresh();
            Assert.False(bean.IsRunning);
            await processor.Start();
            Assert.True(bean.IsRunning);
            await processor.Stop();
            var stopped = stoppedBeans.ToArray();
            Assert.False(bean.IsRunning);
            Assert.Same(bean, stopped[0]);
        }

        [Fact]
        public async Task MixedShutdown()
        {
            var stoppedBeans = new ConcurrentQueue<ILifecycle>();
            ILifecycle bean1 = TestLifecycleBean.ForShutdownTests(stoppedBeans);
            ILifecycle bean2 = TestSmartLifecycleBean.ForShutdownTests(500, 200, stoppedBeans);
            ILifecycle bean3 = TestSmartLifecycleBean.ForShutdownTests(int.MaxValue, 100, stoppedBeans);
            ILifecycle bean4 = TestLifecycleBean.ForShutdownTests(stoppedBeans);
            ILifecycle bean5 = TestSmartLifecycleBean.ForShutdownTests(1, 200, stoppedBeans);
            ILifecycle bean6 = TestSmartLifecycleBean.ForShutdownTests(-1, 100, stoppedBeans);
            ILifecycle bean7 = TestSmartLifecycleBean.ForShutdownTests(int.MinValue, 300, stoppedBeans);

            var processor = new DefaultLifecycleProcessor(new List<ILifecycle>() { bean1, bean2, bean3, bean4, bean5, bean6, bean7 }, new List<ISmartLifecycle>());
            await processor.OnRefresh();

            Assert.True(bean2.IsRunning);
            Assert.True(bean3.IsRunning);
            Assert.True(bean5.IsRunning);
            Assert.True(bean6.IsRunning);
            Assert.True(bean7.IsRunning);
            Assert.False(bean1.IsRunning);
            Assert.False(bean4.IsRunning);

            await bean1.Start();
            await bean4.Start();

            Assert.True(bean1.IsRunning);
            Assert.True(bean4.IsRunning);

            await processor.Stop();

            Assert.False(bean2.IsRunning);
            Assert.False(bean3.IsRunning);
            Assert.False(bean5.IsRunning);
            Assert.False(bean6.IsRunning);
            Assert.False(bean7.IsRunning);
            Assert.False(bean1.IsRunning);
            Assert.False(bean4.IsRunning);

            var stopped = stoppedBeans.ToArray();
            Assert.Equal(7, stopped.Length);
            Assert.Equal(int.MaxValue, GetPhase(stopped[0]));
            Assert.Equal(500, GetPhase(stopped[1]));
            Assert.Equal(1, GetPhase(stopped[2]));
            Assert.Equal(0, GetPhase(stopped[3]));
            Assert.Equal(0, GetPhase(stopped[4]));
            Assert.Equal(-1, GetPhase(stopped[5]));
            Assert.Equal(int.MinValue, GetPhase(stopped[6]));
        }

        private static int GetPhase(ILifecycle lifecycle)
        {
            if (lifecycle is ISmartLifecycle)
            {
                return ((ISmartLifecycle)lifecycle).Phase;
            }

            return 0;
        }

        private class TestLifecycleBean : ILifecycle
        {
            private readonly ConcurrentQueue<ILifecycle> startedBeans;
            private readonly ConcurrentQueue<ILifecycle> stoppedBeans;

            public static TestLifecycleBean ForStartupTests(ConcurrentQueue<ILifecycle> startedBeans)
            {
                return new TestLifecycleBean(startedBeans, null);
            }

            public static TestLifecycleBean ForShutdownTests(ConcurrentQueue<ILifecycle> stoppedBeans)
            {
                return new TestLifecycleBean(null, stoppedBeans);
            }

            public TestLifecycleBean(ConcurrentQueue<ILifecycle> startedBeans, ConcurrentQueue<ILifecycle> stoppedBeans)
            {
                this.startedBeans = startedBeans;
                this.stoppedBeans = stoppedBeans;
            }

            public bool IsRunning { get; private set; }

            public Task Start()
            {
                if (startedBeans != null)
                {
                    startedBeans.Enqueue(this);
                }

                IsRunning = true;
                return Task.CompletedTask;
            }

            public Task Stop()
            {
                if (stoppedBeans != null)
                {
                    stoppedBeans.Enqueue(this);
                }

                IsRunning = false;
                return Task.CompletedTask;
            }
        }

        private class TestSmartLifecycleBean : TestLifecycleBean, ISmartLifecycle
        {
            private readonly int shutdownDelay;

            public bool IsAutoStartup { get; set; } = true;

            public int Phase { get; private set; }

            public static TestSmartLifecycleBean ForStartupTests(int phase, ConcurrentQueue<ILifecycle> startedBeans)
            {
                return new TestSmartLifecycleBean(phase, 0, startedBeans, null);
            }

            public static TestSmartLifecycleBean ForShutdownTests(int phase, int shutdownDelay, ConcurrentQueue<ILifecycle> stoppedBeans)
            {
                return new TestSmartLifecycleBean(phase, shutdownDelay, null, stoppedBeans);
            }

            public async Task Stop(Action callback)
            {
                await Stop();
                var delay = shutdownDelay;
                await Task.Delay(delay);
                callback();
            }

            public TestSmartLifecycleBean(int phase, int shutdownDelay, ConcurrentQueue<ILifecycle> startedBeans, ConcurrentQueue<ILifecycle> stoppedBeans)
                : base(startedBeans, stoppedBeans)
            {
                Phase = phase;
                this.shutdownDelay = shutdownDelay;
            }
        }
    }
}

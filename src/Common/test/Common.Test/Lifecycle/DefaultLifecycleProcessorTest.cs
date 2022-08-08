// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Xunit;

namespace Steeltoe.Common.Test.Lifecycle;

public class DefaultLifecycleProcessorTest
{
    [Fact]
    public async Task SingleSmartLifecycleAutoStartup()
    {
        var startedBeans = new ConcurrentQueue<ILifecycle>();
        TestSmartLifecycleBean bean = TestSmartLifecycleBean.ForStartupTests(1, startedBeans);
        bean.IsAutoStartup = true;

        var processor = new DefaultLifecycleProcessor(CreateApplicationContext(new List<ILifecycle>
        {
            bean
        }, new List<ISmartLifecycle>()));

        Assert.False(bean.IsRunning);
        await processor.OnRefreshAsync();
        Assert.True(bean.IsRunning);
        await processor.StopAsync();
        Assert.False(bean.IsRunning);
        Assert.Single(startedBeans);
    }

    [Fact]
    public async Task SingleSmartLifecycleWithoutAutoStartup()
    {
        var startedBeans = new ConcurrentQueue<ILifecycle>();
        TestSmartLifecycleBean bean = TestSmartLifecycleBean.ForStartupTests(1, startedBeans);
        bean.IsAutoStartup = false;

        var processor = new DefaultLifecycleProcessor(CreateApplicationContext(new List<ILifecycle>
        {
            bean
        }, new List<ISmartLifecycle>()));

        Assert.False(bean.IsRunning);
        await processor.OnRefreshAsync();
        Assert.False(bean.IsRunning);
        Assert.Empty(startedBeans);
        await processor.StartAsync();
        Assert.True(bean.IsRunning);
        Assert.Single(startedBeans);
        await processor.StopAsync();
    }

    [Fact]
    public async Task SmartLifecycleGroupStartup()
    {
        var startedBeans = new ConcurrentQueue<ILifecycle>();
        TestSmartLifecycleBean beanMin = TestSmartLifecycleBean.ForStartupTests(int.MinValue, startedBeans);
        TestSmartLifecycleBean bean1 = TestSmartLifecycleBean.ForStartupTests(1, startedBeans);
        TestSmartLifecycleBean bean2 = TestSmartLifecycleBean.ForStartupTests(2, startedBeans);
        TestSmartLifecycleBean bean3 = TestSmartLifecycleBean.ForStartupTests(3, startedBeans);
        TestSmartLifecycleBean beanMax = TestSmartLifecycleBean.ForStartupTests(int.MaxValue, startedBeans);

        var processor = new DefaultLifecycleProcessor(CreateApplicationContext(new List<ILifecycle>
        {
            bean3,
            beanMin,
            bean2,
            beanMax,
            bean1
        }, new List<ISmartLifecycle>()));

        Assert.False(beanMin.IsRunning);
        Assert.False(bean1.IsRunning);
        Assert.False(bean2.IsRunning);
        Assert.False(bean3.IsRunning);
        Assert.False(beanMax.IsRunning);

        await processor.OnRefreshAsync();

        Assert.True(beanMin.IsRunning);
        Assert.True(bean1.IsRunning);
        Assert.True(bean2.IsRunning);
        Assert.True(bean3.IsRunning);
        Assert.True(beanMax.IsRunning);

        await processor.StopAsync();

        Assert.Equal(5, startedBeans.Count);
        ILifecycle[] started = startedBeans.ToArray();
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
        TestLifecycleBean simpleBean1 = TestLifecycleBean.ForStartupTests(startedBeans);
        TestLifecycleBean simpleBean2 = TestLifecycleBean.ForStartupTests(startedBeans);
        TestSmartLifecycleBean smartBean1 = TestSmartLifecycleBean.ForStartupTests(5, startedBeans);
        TestSmartLifecycleBean smartBean2 = TestSmartLifecycleBean.ForStartupTests(-3, startedBeans);

        var processor = new DefaultLifecycleProcessor(CreateApplicationContext(new List<ILifecycle>
        {
            simpleBean1,
            simpleBean2,
            smartBean1,
            smartBean2
        }, new List<ISmartLifecycle>()));

        Assert.False(simpleBean1.IsRunning);
        Assert.False(simpleBean2.IsRunning);
        Assert.False(smartBean1.IsRunning);
        Assert.False(smartBean2.IsRunning);

        await processor.OnRefreshAsync();

        Assert.False(simpleBean1.IsRunning);
        Assert.False(simpleBean2.IsRunning);
        Assert.True(smartBean1.IsRunning);
        Assert.True(smartBean2.IsRunning);

        Assert.Equal(2, startedBeans.Count);
        ILifecycle[] started = startedBeans.ToArray();
        Assert.Equal(-3, GetPhase(started[0]));
        Assert.Equal(5, GetPhase(started[1]));

        await processor.StartAsync();

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
        TestLifecycleBean simpleBean1 = TestLifecycleBean.ForStartupTests(startedBeans);
        TestLifecycleBean simpleBean2 = TestLifecycleBean.ForStartupTests(startedBeans);
        TestSmartLifecycleBean smartBean1 = TestSmartLifecycleBean.ForStartupTests(5, startedBeans);
        TestSmartLifecycleBean smartBean2 = TestSmartLifecycleBean.ForStartupTests(-3, startedBeans);

        var processor = new DefaultLifecycleProcessor(CreateApplicationContext(new List<ILifecycle>
        {
            simpleBean1,
            simpleBean2,
            smartBean1,
            smartBean2
        }, new List<ISmartLifecycle>()));

        Assert.False(simpleBean1.IsRunning);
        Assert.False(simpleBean2.IsRunning);
        Assert.False(smartBean1.IsRunning);
        Assert.False(smartBean2.IsRunning);

        await processor.OnRefreshAsync();

        Assert.False(simpleBean1.IsRunning);
        Assert.False(simpleBean2.IsRunning);
        Assert.True(smartBean1.IsRunning);
        Assert.True(smartBean2.IsRunning);

        Assert.Equal(2, startedBeans.Count);
        ILifecycle[] started = startedBeans.ToArray();
        Assert.Equal(-3, GetPhase(started[0]));
        Assert.Equal(5, GetPhase(started[1]));

        await processor.StopAsync();

        Assert.False(simpleBean1.IsRunning);
        Assert.False(simpleBean2.IsRunning);
        Assert.False(smartBean1.IsRunning);
        Assert.False(smartBean2.IsRunning);

        await processor.StartAsync();

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

        TestSmartLifecycleBean bean1 = TestSmartLifecycleBean.ForShutdownTests(1, 300, stoppedBeans);
        TestSmartLifecycleBean bean2 = TestSmartLifecycleBean.ForShutdownTests(3, 100, stoppedBeans);
        TestSmartLifecycleBean bean3 = TestSmartLifecycleBean.ForShutdownTests(1, 600, stoppedBeans);
        TestSmartLifecycleBean bean4 = TestSmartLifecycleBean.ForShutdownTests(2, 400, stoppedBeans);
        TestSmartLifecycleBean bean5 = TestSmartLifecycleBean.ForShutdownTests(2, 700, stoppedBeans);
        TestSmartLifecycleBean bean6 = TestSmartLifecycleBean.ForShutdownTests(int.MaxValue, 200, stoppedBeans);
        TestSmartLifecycleBean bean7 = TestSmartLifecycleBean.ForShutdownTests(3, 200, stoppedBeans);

        var processor = new DefaultLifecycleProcessor(CreateApplicationContext(new List<ILifecycle>
        {
            bean1,
            bean2,
            bean3,
            bean4,
            bean5,
            bean6,
            bean7
        }, new List<ISmartLifecycle>()));

        await processor.OnRefreshAsync();
        await processor.StopAsync();
        ILifecycle[] stopped = stoppedBeans.ToArray();
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
        TestSmartLifecycleBean bean = TestSmartLifecycleBean.ForShutdownTests(99, 300, stoppedBeans);

        var processor = new DefaultLifecycleProcessor(CreateApplicationContext(new List<ILifecycle>
        {
            bean
        }, new List<ISmartLifecycle>()));

        await processor.OnRefreshAsync();
        Assert.True(bean.IsRunning);
        await processor.StopAsync();
        ILifecycle[] stopped = stoppedBeans.ToArray();
        Assert.Same(bean, stopped[0]);
    }

    [Fact]
    public async Task SingleLifecycleShutdown()
    {
        var stoppedBeans = new ConcurrentQueue<ILifecycle>();
        ILifecycle bean = new TestLifecycleBean(null, stoppedBeans);

        var processor = new DefaultLifecycleProcessor(CreateApplicationContext(new List<ILifecycle>
        {
            bean
        }, new List<ISmartLifecycle>()));

        Assert.False(bean.IsRunning);
        await processor.OnRefreshAsync();
        Assert.False(bean.IsRunning);
        await processor.StartAsync();
        Assert.True(bean.IsRunning);
        await processor.StopAsync();
        ILifecycle[] stopped = stoppedBeans.ToArray();
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

        var processor = new DefaultLifecycleProcessor(CreateApplicationContext(new List<ILifecycle>
        {
            bean1,
            bean2,
            bean3,
            bean4,
            bean5,
            bean6,
            bean7
        }, new List<ISmartLifecycle>()));

        await processor.OnRefreshAsync();

        Assert.True(bean2.IsRunning);
        Assert.True(bean3.IsRunning);
        Assert.True(bean5.IsRunning);
        Assert.True(bean6.IsRunning);
        Assert.True(bean7.IsRunning);
        Assert.False(bean1.IsRunning);
        Assert.False(bean4.IsRunning);

        await bean1.StartAsync();
        await bean4.StartAsync();

        Assert.True(bean1.IsRunning);
        Assert.True(bean4.IsRunning);

        await processor.StopAsync();

        Assert.False(bean2.IsRunning);
        Assert.False(bean3.IsRunning);
        Assert.False(bean5.IsRunning);
        Assert.False(bean6.IsRunning);
        Assert.False(bean7.IsRunning);
        Assert.False(bean1.IsRunning);
        Assert.False(bean4.IsRunning);

        ILifecycle[] stopped = stoppedBeans.ToArray();
        Assert.Equal(7, stopped.Length);
        Assert.Equal(int.MaxValue, GetPhase(stopped[0]));
        Assert.Equal(500, GetPhase(stopped[1]));
        Assert.Equal(1, GetPhase(stopped[2]));
        Assert.Equal(0, GetPhase(stopped[3]));
        Assert.Equal(0, GetPhase(stopped[4]));
        Assert.Equal(-1, GetPhase(stopped[5]));
        Assert.Equal(int.MinValue, GetPhase(stopped[6]));
    }

    private static IApplicationContext CreateApplicationContext(List<ILifecycle> lifecycles, List<ISmartLifecycle> smartLifecycles)
    {
        IConfigurationRoot config = new ConfigurationBuilder().Build();
        var serviceCollection = new ServiceCollection();

        foreach (ILifecycle lifeCycle in lifecycles)
        {
            serviceCollection.AddSingleton(lifeCycle);
        }

        foreach (ISmartLifecycle lifeCycle in smartLifecycles)
        {
            serviceCollection.AddSingleton(lifeCycle);
        }

        return new GenericApplicationContext(serviceCollection.BuildServiceProvider(), config);
    }

    private static int GetPhase(ILifecycle lifecycle)
    {
        if (lifecycle is ISmartLifecycle lifecycle1)
        {
            return lifecycle1.Phase;
        }

        return 0;
    }

    private class TestLifecycleBean : ILifecycle
    {
        private readonly ConcurrentQueue<ILifecycle> _startedBeans;
        private readonly ConcurrentQueue<ILifecycle> _stoppedBeans;

        public bool IsRunning { get; private set; }

        public TestLifecycleBean(ConcurrentQueue<ILifecycle> startedBeans, ConcurrentQueue<ILifecycle> stoppedBeans)
        {
            _startedBeans = startedBeans;
            _stoppedBeans = stoppedBeans;
        }

        public static TestLifecycleBean ForStartupTests(ConcurrentQueue<ILifecycle> startedBeans)
        {
            return new TestLifecycleBean(startedBeans, null);
        }

        public static TestLifecycleBean ForShutdownTests(ConcurrentQueue<ILifecycle> stoppedBeans)
        {
            return new TestLifecycleBean(null, stoppedBeans);
        }

        public Task StartAsync()
        {
            if (_startedBeans != null)
            {
                _startedBeans.Enqueue(this);
            }

            IsRunning = true;
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            if (_stoppedBeans != null)
            {
                _stoppedBeans.Enqueue(this);
            }

            IsRunning = false;
            return Task.CompletedTask;
        }
    }

    private sealed class TestSmartLifecycleBean : TestLifecycleBean, ISmartLifecycle
    {
        private readonly int _shutdownDelay;

        public bool IsAutoStartup { get; set; } = true;

        public int Phase { get; }

        public TestSmartLifecycleBean(int phase, int shutdownDelay, ConcurrentQueue<ILifecycle> startedBeans, ConcurrentQueue<ILifecycle> stoppedBeans)
            : base(startedBeans, stoppedBeans)
        {
            Phase = phase;
            _shutdownDelay = shutdownDelay;
        }

        public static TestSmartLifecycleBean ForStartupTests(int phase, ConcurrentQueue<ILifecycle> startedBeans)
        {
            return new TestSmartLifecycleBean(phase, 0, startedBeans, null);
        }

        public static TestSmartLifecycleBean ForShutdownTests(int phase, int shutdownDelay, ConcurrentQueue<ILifecycle> stoppedBeans)
        {
            return new TestSmartLifecycleBean(phase, shutdownDelay, null, stoppedBeans);
        }

        public async Task StopAsync(Action callback)
        {
            await StopAsync();
            int delay = _shutdownDelay;
            await Task.Delay(delay);
            callback();
        }
    }
}

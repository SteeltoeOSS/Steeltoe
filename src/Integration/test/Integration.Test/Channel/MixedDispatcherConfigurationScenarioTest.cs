// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Dispatcher;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using Xunit;

// TODO: Fix violations and remove the next suppression, by either:
// - Removing the try with empty catch block
// - Add the next comment in the empty catch block: // Intentionally left empty.
// While you're at it, catch specific exceptions (use `when` condition to narrow down) instead of System.Exception.
#pragma warning disable S108 // Nested blocks of code should not be left empty

namespace Steeltoe.Integration.Channel.Test;

public class MixedDispatcherConfigurationScenarioTest
{
    private const int TotalExecutions = 40;
    private readonly IMessage _message = Message.Create("test");
    private readonly CountdownEvent _allDone;
    private readonly CountdownEvent _start;
    private readonly Mock<IMessageHandler> _handlerA;
    private readonly Mock<IMessageHandler> _handlerB;
    private readonly Mock<IMessageHandler> _handlerC;
    private readonly Mock<IList<Exception>> _exceptionRegistry;
    private readonly IServiceProvider _provider;
    private int _failed;

    public MixedDispatcherConfigurationScenarioTest()
    {
        var services = new ServiceCollection();
        IConfigurationRoot config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton<IApplicationContext, GenericApplicationContext>();
        services.AddSingleton<IDestinationResolver<IMessageChannel>, DefaultMessageChannelDestinationResolver>();
        services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        _provider = services.BuildServiceProvider();

        _handlerA = new Mock<IMessageHandler>();
        _handlerB = new Mock<IMessageHandler>();
        _handlerC = new Mock<IMessageHandler>();
        _allDone = new CountdownEvent(TotalExecutions);
        _start = new CountdownEvent(1);
        _failed = 0;
        _exceptionRegistry = new Mock<IList<Exception>>();
    }

    [Fact]
    public void NoFailoverNoLoadBalancing()
    {
        var channel = new DirectChannel(_provider.GetService<IApplicationContext>(), null, "noLoadBalancerNoFailover")
        {
            Failover = false
        };

        _handlerA.Setup(h => h.HandleMessage(_message)).Throws(new MessageRejectedException(_message, null));
        IMessageDispatcher dispatcher = channel.Dispatcher;
        dispatcher.AddHandler(_handlerA.Object);
        dispatcher.AddHandler(_handlerB.Object);

        try
        {
            channel.Send(_message);
        }
        catch (Exception)
        {
        }

        try
        {
            channel.Send(_message);
        }
        catch (Exception)
        {
        }

        _handlerA.Verify(h => h.HandleMessage(_message), Times.Exactly(2));
        _handlerB.Verify(h => h.HandleMessage(_message), Times.Exactly(0));
    }

    [Fact]
    public void NoFailoverNoLoadBalancingConcurrent()
    {
        var channel = new DirectChannel(_provider.GetService<IApplicationContext>(), null, "noLoadBalancerNoFailover")
        {
            Failover = false
        };

        _handlerA.Setup(h => h.HandleMessage(_message)).Throws(new MessageRejectedException(_message, null));
        IMessageDispatcher dispatcher = channel.Dispatcher;
        dispatcher.AddHandler(_handlerA.Object);
        dispatcher.AddHandler(_handlerB.Object);

        void MessageSenderTask()
        {
            _start.Wait();

            bool sent = false;

            try
            {
                sent = channel.Send(_message);
            }
            catch (Exception e2)
            {
                _exceptionRegistry.Object.Add(e2);
            }

            if (!sent)
            {
                _failed = 1;
            }

            _allDone.Signal();
        }

        for (int i = 0; i < TotalExecutions; i++)
        {
            Task.Run(MessageSenderTask);
        }

        _start.Signal();
        Assert.True(_allDone.Wait(10000));

        Assert.Equal(1, _failed);
        _handlerA.Verify(h => h.HandleMessage(_message), Times.Exactly(TotalExecutions));
        _handlerB.Verify(h => h.HandleMessage(_message), Times.Exactly(0));

        _exceptionRegistry.Verify(list => list.Add(It.IsAny<Exception>()), Times.Exactly(TotalExecutions));
    }

    [Fact]
    public void NoFailoverNoLoadBalancingWithExecutorConcurrent()
    {
        var channel = new TaskSchedulerChannel(_provider.GetService<IApplicationContext>(), TaskScheduler.Default, null, "noLoadBalancerNoFailoverExecutor")
        {
            Failover = false
        };

        _handlerA.Setup(h => h.HandleMessage(_message)).Callback(() =>
        {
            var e = new Exception();
            _failed = 1;
            _exceptionRegistry.Object.Add(e);
            _allDone.Signal();
            throw e;
        });

        IMessageDispatcher dispatcher = channel.Dispatcher;
        dispatcher.AddHandler(_handlerA.Object);
        dispatcher.AddHandler(_handlerB.Object);

        void MessageSenderTask()
        {
            _start.Wait();

            channel.Send(_message);
        }

        for (int i = 0; i < TotalExecutions; i++)
        {
            Task.Run(MessageSenderTask);
        }

        _start.Signal();
        Assert.True(_allDone.Wait(10000));

        Assert.Equal(1, _failed);
        _handlerA.Verify(h => h.HandleMessage(_message), Times.Exactly(TotalExecutions));
        _handlerB.Verify(h => h.HandleMessage(_message), Times.Exactly(0));

        _exceptionRegistry.Verify(list => list.Add(It.IsAny<Exception>()), Times.Exactly(TotalExecutions));
    }

    [Fact]
    public void NoFailoverLoadBalancing()
    {
        var channel = new DirectChannel(_provider.GetService<IApplicationContext>(), "loadBalancerNoFailover")
        {
            Failover = false
        };

        _handlerA.Setup(h => h.HandleMessage(_message)).Throws(new MessageRejectedException(_message, null));
        IMessageDispatcher dispatcher = channel.Dispatcher;
        dispatcher.AddHandler(_handlerA.Object);
        dispatcher.AddHandler(_handlerB.Object);
        dispatcher.AddHandler(_handlerC.Object);

        try
        {
            channel.Send(_message);
        }
        catch (Exception)
        {
        }

        _handlerA.Verify(h => h.HandleMessage(_message), Times.Exactly(1));
        _handlerB.Verify(h => h.HandleMessage(_message), Times.Exactly(0));
        _handlerC.Verify(h => h.HandleMessage(_message), Times.Exactly(0));

        try
        {
            channel.Send(_message);
        }
        catch (Exception)
        {
        }

        _handlerA.Verify(h => h.HandleMessage(_message), Times.Exactly(1));
        _handlerB.Verify(h => h.HandleMessage(_message), Times.Exactly(1));
        _handlerC.Verify(h => h.HandleMessage(_message), Times.Exactly(0));

        try
        {
            channel.Send(_message);
        }
        catch (Exception)
        {
        }

        _handlerA.Verify(h => h.HandleMessage(_message), Times.Exactly(1));
        _handlerB.Verify(h => h.HandleMessage(_message), Times.Exactly(1));
        _handlerC.Verify(h => h.HandleMessage(_message), Times.Exactly(1));
    }

    [Fact]
    public void NoFailoverLoadBalancingConcurrent()
    {
        var channel = new DirectChannel(_provider.GetService<IApplicationContext>(), "loadBalancerNoFailover")
        {
            Failover = false
        };

        _handlerA.Setup(h => h.HandleMessage(_message)).Throws(new MessageRejectedException(_message, null));
        IMessageDispatcher dispatcher = channel.Dispatcher;
        dispatcher.AddHandler(_handlerA.Object);
        dispatcher.AddHandler(_handlerB.Object);
        dispatcher.AddHandler(_handlerC.Object);

        var start1 = new CountdownEvent(1);
        var allDone1 = new CountdownEvent(TotalExecutions);
        IMessage message2 = _message;
        int failed1 = 0;

        void MessageSenderTask()
        {
            start1.Wait();

            bool sent = false;

            try
            {
                sent = channel.Send(message2);
            }
            catch (Exception e2)
            {
                _exceptionRegistry.Object.Add(e2);
            }

            if (!sent)
            {
                failed1 = 1;
            }

            allDone1.Signal();
        }

        for (int i = 0; i < TotalExecutions; i++)
        {
            Task.Run(MessageSenderTask);
        }

        start1.Signal();
        Assert.True(allDone1.Wait(10000));

        Assert.Equal(1, failed1);
        _handlerA.Verify(h => h.HandleMessage(message2), Times.Exactly(14));
        _handlerB.Verify(h => h.HandleMessage(message2), Times.Exactly(13));
        _handlerC.Verify(h => h.HandleMessage(message2), Times.Exactly(13));
        _exceptionRegistry.Verify(list => list.Add(It.IsAny<Exception>()), Times.Exactly(14));
    }

    [Fact]
    public void NoFailoverLoadBalancingWithExecutorConcurrent()
    {
        var channel = new TaskSchedulerChannel(_provider.GetService<IApplicationContext>(), TaskScheduler.Default)
        {
            Failover = false
        };

        IMessageDispatcher dispatcher = channel.Dispatcher;
        dispatcher.AddHandler(_handlerA.Object);
        dispatcher.AddHandler(_handlerB.Object);
        dispatcher.AddHandler(_handlerC.Object);

        var start1 = new CountdownEvent(1);
        var allDone1 = new CountdownEvent(TotalExecutions);
        IMessage message2 = _message;
        int failed1 = 0;

        _handlerA.Setup(h => h.HandleMessage(_message)).Callback(() =>
        {
            failed1 = 1;
            var e = new Exception();
            _exceptionRegistry.Object.Add(e);
            allDone1.Signal();
            throw e;
        });

        _handlerB.Setup(h => h.HandleMessage(_message)).Callback(() =>
        {
            allDone1.Signal();
        });

        _handlerC.Setup(h => h.HandleMessage(_message)).Callback(() =>
        {
            allDone1.Signal();
        });

        void MessageSenderTask()
        {
            start1.Wait();

            channel.Send(_message);
        }

        for (int i = 0; i < TotalExecutions; i++)
        {
            Task.Run(MessageSenderTask);
        }

        start1.Signal();
        Assert.True(allDone1.Wait(10000));
        Assert.Equal(1, failed1);
        _handlerA.Verify(h => h.HandleMessage(message2), Times.Exactly(14));
        _handlerB.Verify(h => h.HandleMessage(message2), Times.Exactly(13));
        _handlerC.Verify(h => h.HandleMessage(message2), Times.Exactly(13));
        _exceptionRegistry.Verify(list => list.Add(It.IsAny<Exception>()), Times.Exactly(14));
    }

    [Fact]
    public void FailoverNoLoadBalancing()
    {
        var channel = new DirectChannel(_provider.GetService<IApplicationContext>(), null, "loadBalancerNoFailover")
        {
            Failover = true
        };

        _handlerA.Setup(h => h.HandleMessage(_message)).Throws(new MessageRejectedException(_message, null));
        IMessageDispatcher dispatcher = channel.Dispatcher;
        dispatcher.AddHandler(_handlerA.Object);
        dispatcher.AddHandler(_handlerB.Object);

        try
        {
            channel.Send(_message);
        }
        catch (Exception)
        {
            /* ignore */
        }

        _handlerA.Verify(h => h.HandleMessage(_message), Times.Exactly(1));
        _handlerB.Verify(h => h.HandleMessage(_message), Times.Exactly(1));

        try
        {
            channel.Send(_message);
        }
        catch (Exception)
        {
            /* ignore */
        }

        _handlerA.Verify(h => h.HandleMessage(_message), Times.Exactly(2));
        _handlerB.Verify(h => h.HandleMessage(_message), Times.Exactly(2));
    }

    [Fact]
    public void FailoverNoLoadBalancingConcurrent()
    {
        var channel = new DirectChannel(_provider.GetService<IApplicationContext>(), null, "noLoadBalancerFailover")
        {
            Failover = true
        };

        _handlerA.Setup(h => h.HandleMessage(_message)).Throws(new MessageRejectedException(_message, null));
        IMessageDispatcher dispatcher = channel.Dispatcher;
        dispatcher.AddHandler(_handlerA.Object);
        dispatcher.AddHandler(_handlerB.Object);
        dispatcher.AddHandler(_handlerC.Object);

        var start1 = new CountdownEvent(1);
        var allDone1 = new CountdownEvent(TotalExecutions);
        IMessage message2 = _message;
        int failed1 = 0;

        void MessageSenderTask()
        {
            start1.Wait();

            bool sent = false;

            try
            {
                sent = channel.Send(message2);
            }
            catch (Exception e2)
            {
                _exceptionRegistry.Object.Add(e2);
            }

            if (!sent)
            {
                failed1 = 1;
            }

            allDone1.Signal();
        }

        for (int i = 0; i < TotalExecutions; i++)
        {
            Task.Run(MessageSenderTask);
        }

        start1.Signal();
        Assert.True(allDone1.Wait(10000));
        Assert.Equal(0, failed1);
        _handlerA.Verify(h => h.HandleMessage(message2), Times.Exactly(TotalExecutions));
        _handlerB.Verify(h => h.HandleMessage(message2), Times.Exactly(TotalExecutions));
        _handlerC.Verify(h => h.HandleMessage(message2), Times.Never());
        _exceptionRegistry.Verify(list => list.Add(It.IsAny<Exception>()), Times.Never());
    }

    [Fact]
    public void FailoverNoLoadBalancingWithExecutorConcurrent()
    {
        var channel = new TaskSchedulerChannel(_provider.GetService<IApplicationContext>(), TaskScheduler.Default, null, null)
        {
            Failover = true
        };

        IMessageDispatcher dispatcher = channel.Dispatcher;
        dispatcher.AddHandler(_handlerA.Object);
        dispatcher.AddHandler(_handlerB.Object);
        dispatcher.AddHandler(_handlerC.Object);

        _handlerA.Setup(h => h.HandleMessage(_message)).Callback(() =>
        {
            _failed = 1;
            var e = new Exception();
            throw e;
        });

        _handlerB.Setup(h => h.HandleMessage(_message)).Callback(() =>
        {
            _allDone.Signal();
        });

        void MessageSenderTask()
        {
            _start.Wait();

            channel.Send(_message);
        }

        for (int i = 0; i < TotalExecutions; i++)
        {
            Task.Run(MessageSenderTask);
        }

        _start.Signal();
        Assert.True(_allDone.Wait(10000));
        _handlerA.Verify(h => h.HandleMessage(_message), Times.Exactly(TotalExecutions));
        _handlerB.Verify(h => h.HandleMessage(_message), Times.Exactly(TotalExecutions));
        _handlerC.Verify(h => h.HandleMessage(_message), Times.Never());
    }
}

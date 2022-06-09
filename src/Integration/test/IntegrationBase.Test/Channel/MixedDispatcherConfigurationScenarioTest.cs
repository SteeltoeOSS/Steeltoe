// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Integration.Channel.Test;

public class MixedDispatcherConfigurationScenarioTest
{
    private const int TOTAL_EXECUTIONS = 40;
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
        var config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton<IApplicationContext, GenericApplicationContext>();
        services.AddSingleton<IDestinationResolver<IMessageChannel>, DefaultMessageChannelDestinationResolver>();
        services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        _provider = services.BuildServiceProvider();

        _handlerA = new Mock<IMessageHandler>();
        _handlerB = new Mock<IMessageHandler>();
        _handlerC = new Mock<IMessageHandler>();
        _allDone = new CountdownEvent(TOTAL_EXECUTIONS);
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
        var dispatcher = channel.Dispatcher;
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
        var dispatcher = channel.Dispatcher;
        dispatcher.AddHandler(_handlerA.Object);
        dispatcher.AddHandler(_handlerB.Object);

        void MessageSenderTask()
        {
            _start.Wait();

            var sent = false;
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

        for (var i = 0; i < TOTAL_EXECUTIONS; i++)
        {
            Task.Run(MessageSenderTask);
        }

        _start.Signal();
        Assert.True(_allDone.Wait(10000));

        Assert.Equal(1, _failed);
        _handlerA.Verify(h => h.HandleMessage(_message), Times.Exactly(TOTAL_EXECUTIONS));
        _handlerB.Verify(h => h.HandleMessage(_message), Times.Exactly(0));

        _exceptionRegistry.Verify(list => list.Add(It.IsAny<Exception>()), Times.Exactly(TOTAL_EXECUTIONS));
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
        var dispatcher = channel.Dispatcher;
        dispatcher.AddHandler(_handlerA.Object);
        dispatcher.AddHandler(_handlerB.Object);

        void MessageSenderTask()
        {
            _start.Wait();

            channel.Send(_message);
        }

        for (var i = 0; i < TOTAL_EXECUTIONS; i++)
        {
            Task.Run(MessageSenderTask);
        }

        _start.Signal();
        Assert.True(_allDone.Wait(10000));

        Assert.Equal(1, _failed);
        _handlerA.Verify(h => h.HandleMessage(_message), Times.Exactly(TOTAL_EXECUTIONS));
        _handlerB.Verify(h => h.HandleMessage(_message), Times.Exactly(0));

        _exceptionRegistry.Verify(list => list.Add(It.IsAny<Exception>()), Times.Exactly(TOTAL_EXECUTIONS));
    }

    [Fact]
    public void NoFailoverLoadBalancing()
    {
        var channel = new DirectChannel(_provider.GetService<IApplicationContext>(), "loadBalancerNoFailover")
        {
            Failover = false
        };
        _handlerA.Setup(h => h.HandleMessage(_message)).Throws(new MessageRejectedException(_message, null));
        var dispatcher = channel.Dispatcher;
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
        var dispatcher = channel.Dispatcher;
        dispatcher.AddHandler(_handlerA.Object);
        dispatcher.AddHandler(_handlerB.Object);
        dispatcher.AddHandler(_handlerC.Object);

        var start1 = new CountdownEvent(1);
        var allDone1 = new CountdownEvent(TOTAL_EXECUTIONS);
        var message2 = _message;
        var failed1 = 0;
        void MessageSenderTask()
        {
            start1.Wait();

            var sent = false;
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

        for (var i = 0; i < TOTAL_EXECUTIONS; i++)
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

        var dispatcher = channel.Dispatcher;
        dispatcher.AddHandler(_handlerA.Object);
        dispatcher.AddHandler(_handlerB.Object);
        dispatcher.AddHandler(_handlerC.Object);

        var start1 = new CountdownEvent(1);
        var allDone1 = new CountdownEvent(TOTAL_EXECUTIONS);
        var message2 = _message;
        var failed1 = 0;
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

        for (var i = 0; i < TOTAL_EXECUTIONS; i++)
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
        var dispatcher = channel.Dispatcher;
        dispatcher.AddHandler(_handlerA.Object);
        dispatcher.AddHandler(_handlerB.Object);

        try
        {
            channel.Send(_message);
        }
        catch (Exception)
        { /* ignore */
        }

        _handlerA.Verify(h => h.HandleMessage(_message), Times.Exactly(1));
        _handlerB.Verify(h => h.HandleMessage(_message), Times.Exactly(1));

        try
        {
            channel.Send(_message);
        }
        catch (Exception)
        { /* ignore */
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
        var dispatcher = channel.Dispatcher;
        dispatcher.AddHandler(_handlerA.Object);
        dispatcher.AddHandler(_handlerB.Object);
        dispatcher.AddHandler(_handlerC.Object);

        var start1 = new CountdownEvent(1);
        var allDone1 = new CountdownEvent(TOTAL_EXECUTIONS);
        var message2 = _message;
        var failed1 = 0;

        void MessageSenderTask()
        {
            start1.Wait();

            var sent = false;
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

        for (var i = 0; i < TOTAL_EXECUTIONS; i++)
        {
            Task.Run(MessageSenderTask);
        }

        start1.Signal();
        Assert.True(allDone1.Wait(10000));
        Assert.Equal(0, failed1);
        _handlerA.Verify(h => h.HandleMessage(message2), Times.Exactly(TOTAL_EXECUTIONS));
        _handlerB.Verify(h => h.HandleMessage(message2), Times.Exactly(TOTAL_EXECUTIONS));
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

        var dispatcher = channel.Dispatcher;
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

        for (var i = 0; i < TOTAL_EXECUTIONS; i++)
        {
            Task.Run(MessageSenderTask);
        }

        _start.Signal();
        Assert.True(_allDone.Wait(10000));
        _handlerA.Verify(h => h.HandleMessage(_message), Times.Exactly(TOTAL_EXECUTIONS));
        _handlerB.Verify(h => h.HandleMessage(_message), Times.Exactly(TOTAL_EXECUTIONS));
        _handlerC.Verify(h => h.HandleMessage(_message), Times.Never());
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Channel;
using Steeltoe.Integration.Handler;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using Steeltoe.Messaging.Support;
using Xunit;

namespace Steeltoe.Integration.Endpoint.Test;

public class MessageProducerSupportEndpointTest
{
    private readonly ServiceCollection _services;

    public MessageProducerSupportEndpointTest()
    {
        _services = new ServiceCollection();
        _services.AddSingleton<IDestinationResolver<IMessageChannel>, DefaultMessageChannelDestinationResolver>();
        _services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
        _services.AddSingleton<IIntegrationServices, IntegrationServices>();
        var config = new ConfigurationBuilder().Build();
        _services.AddSingleton<IConfiguration>(config);
        _services.AddSingleton<IApplicationContext, GenericApplicationContext>();
    }

    [Fact]
    public async Task ValidateExceptionIfNoErrorChannel()
    {
        var provider = _services.BuildServiceProvider();
        var outChannel = new DirectChannel(provider.GetService<IApplicationContext>());
        var handler = new ExceptionHandler();
        outChannel.Subscribe(handler);

        var mps = new TestMessageProducerSupportEndpoint(provider.GetService<IApplicationContext>())
        {
            OutputChannel = outChannel
        };

        await mps.Start();
        Assert.Throws<MessageDeliveryException>(() => mps.SendMessage(Message.Create("hello")));
    }

    [Fact]
    public async Task ValidateExceptionIfSendToErrorChannelFails()
    {
        var provider = _services.BuildServiceProvider();
        var outChannel = new DirectChannel(provider.GetService<IApplicationContext>());
        var handler = new ExceptionHandler();
        outChannel.Subscribe(handler);
        var errorChannel = new PublishSubscribeChannel(provider.GetService<IApplicationContext>());
        errorChannel.Subscribe(handler);

        var mps = new TestMessageProducerSupportEndpoint(provider.GetService<IApplicationContext>())
        {
            OutputChannel = outChannel,
            ErrorChannel = errorChannel
        };

        await mps.Start();
        Assert.Throws<MessageDeliveryException>(() => mps.SendMessage(Message.Create("hello")));
    }

    [Fact]
    public async Task ValidateSuccessfulErrorFlowDoesNotThrowErrors()
    {
        var provider = _services.BuildServiceProvider();
        var outChannel = new DirectChannel(provider.GetService<IApplicationContext>());
        var handler = new ExceptionHandler();
        outChannel.Subscribe(handler);
        var errorChannel = new PublishSubscribeChannel(provider.GetService<IApplicationContext>());
        var errorService = new SuccessfulErrorService();
        var errorHandler = new ServiceActivatingHandler(provider.GetService<IApplicationContext>(), errorService);
        errorChannel.Subscribe(errorHandler);

        var mps = new TestMessageProducerSupportEndpoint(provider.GetService<IApplicationContext>())
        {
            OutputChannel = outChannel,
            ErrorChannel = errorChannel
        };

        await mps.Start();
        var message = Message.Create("hello");
        mps.SendMessage(message);
        Assert.IsType<ErrorMessage>(errorService.LastMessage);
        Assert.IsType<MessageDeliveryException>(errorService.LastMessage.Payload);
        var exception = (MessageDeliveryException)errorService.LastMessage.Payload;
        Assert.Equal(message, exception.FailedMessage);
    }

    [Fact]
    public async Task TestWithChannelName()
    {
        _services.AddSingleton<IMessageChannel>(p => new DirectChannel(p.GetService<IApplicationContext>(), "foo"));
        var provider = _services.BuildServiceProvider();
        var mps = new TestMessageProducerSupportEndpoint(provider.GetService<IApplicationContext>())
        {
            OutputChannelName = "foo"
        };
        await mps.Start();
        Assert.NotNull(mps.OutputChannel);
        Assert.Equal("foo", mps.OutputChannel.ServiceName);
    }

    [Fact]
    public async Task CustomDoStop()
    {
        var provider = _services.BuildServiceProvider();
        var endpoint = new CustomEndpoint(provider.GetService<IApplicationContext>());
        Assert.Equal(0, endpoint.Count);
        Assert.False(endpoint.IsRunning);
        await endpoint.Start();
        Assert.True(endpoint.IsRunning);
        await endpoint.Stop(() =>
        {
            // Do nothing
        });
        Assert.Equal(1, endpoint.Count);
        Assert.False(endpoint.IsRunning);
    }

    private sealed class TestMessageProducerSupportEndpoint : MessageProducerSupportEndpoint
    {
        public TestMessageProducerSupportEndpoint(IApplicationContext context)
            : base(context)
        {
        }
    }

    private sealed class ExceptionHandler : IMessageHandler
    {
        public string ServiceName { get; set; } = nameof(ExceptionHandler);

        public void HandleMessage(IMessage message)
        {
            throw new Exception("problems");
        }
    }

    private sealed class SuccessfulErrorService : IMessageProcessor
    {
        public volatile IMessage LastMessage;

        public object ProcessMessage(IMessage message)
        {
            LastMessage = message;
            return null;
        }
    }

    private sealed class CustomEndpoint : AbstractEndpoint
    {
        public int Count;
        public bool Stopped;

        public CustomEndpoint(IApplicationContext context)
            : base(context)
        {
        }

        protected override Task DoStart()
        {
            Stopped = false;
            return Task.CompletedTask;
        }

        protected override Task DoStop()
        {
            Stopped = true;
            return Task.CompletedTask;
        }

        protected override Task DoStop(Action callback)
        {
            Count++;
            return base.DoStop(callback);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Support;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Core;

[Trait("Category", "Integration")]
public class RabbitTemplateDirectReplyToContainerIntegrationTest : RabbitTemplateIntegrationTest
{
    [Fact]
    public void ChannelReleasedOnTimeout()
    {
        var connectionFactory = new CachingConnectionFactory("localhost");
        var rabbitTemplate = CreateSendAndReceiveRabbitTemplate(connectionFactory);
        rabbitTemplate.ReplyTimeout = 1;
        var exception = new AtomicReference<Exception>();
        var latch = new CountdownEvent(1);
        rabbitTemplate.ReplyErrorHandler = new TestErrorHandler(exception, latch);
        var reply = rabbitTemplate.ConvertSendAndReceive<object>(Route, "foo");
        Assert.Null(reply);
        var directReplyToContainers = rabbitTemplate.DirectReplyToContainers;
        var container = rabbitTemplate.UsePublisherConnection ? directReplyToContainers[connectionFactory.PublisherConnectionFactory] : directReplyToContainers[connectionFactory];
        Assert.Empty(container.InUseConsumerChannels);
        Assert.Same(rabbitTemplate.ReplyErrorHandler, container.ErrorHandler);
        var replyMessage = Message.Create(Encoding.UTF8.GetBytes("foo"), new MessageHeaders());

        var ex = Assert.Throws<RabbitRejectAndDontRequeueException>(() => rabbitTemplate.OnMessage(replyMessage));
        Assert.Contains("No correlation header in reply", ex.Message);

        var accessor = RabbitHeaderAccessor.GetMutableAccessor(replyMessage);
        accessor.CorrelationId = "foo";

        ex = Assert.Throws<RabbitRejectAndDontRequeueException>(() => rabbitTemplate.OnMessage(replyMessage));
        Assert.Contains("Reply received after timeout", ex.Message);

        _ = Task.Run(() =>
        {
            var message = rabbitTemplate.Receive(Route, 10000);
            Assert.NotNull(message);
            rabbitTemplate.Send(message.Headers.ReplyTo(), replyMessage);
            return message;
        });

        while (rabbitTemplate.Receive(Route, 100) != null)
        {
            // Intentionally left empty.
        }

        reply = rabbitTemplate.ConvertSendAndReceive<object>(Route, "foo");
        Assert.Null(reply);
        Assert.True(latch.Wait(TimeSpan.FromSeconds(10)));
        Assert.IsType<ListenerExecutionFailedException>(exception.Value);
        var listException = exception.Value as ListenerExecutionFailedException;
        Assert.Contains("Reply received after timeout", exception.Value.InnerException.Message);
        Assert.Equal(replyMessage.Payload, listException.FailedMessage.Payload);
        Assert.Empty(container.InUseConsumerChannels);
        rabbitTemplate.Stop().Wait();
        connectionFactory.Destroy();
    }

    protected override RabbitTemplate CreateSendAndReceiveRabbitTemplate(IConnectionFactory connectionFactory)
    {
        var rabbitTemplate = base.CreateSendAndReceiveRabbitTemplate(connectionFactory);
        rabbitTemplate.UseDirectReplyToContainer = true;
        rabbitTemplate.ServiceName = $"{nameof(RabbitTemplateDirectReplyToContainerIntegrationTest)}.SendReceiveRabbitTemplate";
        return rabbitTemplate;
    }

    private sealed class TestErrorHandler : IErrorHandler
    {
        public TestErrorHandler(AtomicReference<Exception> atomicReference, CountdownEvent latch)
        {
            AtomicReference = atomicReference;
            Latch = latch;
        }

        public string ServiceName { get; set; } = nameof(TestErrorHandler);

        public AtomicReference<Exception> AtomicReference { get; }

        public CountdownEvent Latch { get; }

        public bool HandleError(Exception exception)
        {
            AtomicReference.Value = exception;
            Latch.Signal();
            return true;
        }
    }
}

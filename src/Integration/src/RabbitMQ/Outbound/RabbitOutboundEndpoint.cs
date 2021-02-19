// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Integration.RabbitMQ.Support;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using static Steeltoe.Messaging.RabbitMQ.Core.RabbitTemplate;

namespace Steeltoe.Integration.Rabbit.Outbound
{
    public class RabbitOutboundEndpoint : AbstractRabbitOutboundEndpoint, IConfirmCallback, IReturnCallback
    {
        public RabbitOutboundEndpoint(IApplicationContext context, RabbitTemplate rabbitTemplate)
            : base(context)
        {
            if (rabbitTemplate == null)
            {
                throw new ArgumentNullException(nameof(rabbitTemplate));
            }

            Template = rabbitTemplate;
            ConnectionFactory = Template.ConnectionFactory;
        }

        public RabbitTemplate Template { get; }

        public bool ExpectReply { get; set; }

        public bool ShouldWaitForConfirm { get; set; }

        public TimeSpan WaitForConfirmTimeout { get; set; }

        public new void Initialize() => base.Initialize();

        public void Confirm(CorrelationData correlationData, bool ack, string cause)
        {
            HandleConfirm(correlationData, ack, cause);
        }

        public void ReturnedMessage(IMessage<byte[]> message, int replyCode, string replyText, string exchange, string routingKey)
        {
            // no need for null check; we asserted we have a RabbitTemplate in doInit()
            var converter = Template.MessageConverter;
            var returned = BuildReturnedMessage(message, replyCode, replyText, exchange, routingKey, converter);
            ReturnChannel.Send(returned);
        }

        protected override RabbitTemplate GetRabbitTemplate()
        {
            return Template;
        }

        protected override void EndpointInit()
        {
            if (ConfirmCorrelationExpression != null)
            {
                Template.ConfirmCallback = this;
            }

            if (ReturnChannel != null)
            {
                Template.ReturnCallback = this;
            }

            var confirmTimeout = ConfirmTimeout;
            if (confirmTimeout != null)
            {
                WaitForConfirmTimeout = confirmTimeout.Value;
            }
        }

        protected override void DoStop()
        {
            Template.Stop().Wait();
        }

        protected override object HandleRequestMessage(IMessage requestMessage)
        {
            var correlationData = GenerateCorrelationData(requestMessage);
            var exchangeName = GenerateExchangeName(requestMessage);
            var routingKey = GenerateRoutingKey(requestMessage);
            if (ExpectReply)
            {
                return SendAndReceive(exchangeName, routingKey, requestMessage, correlationData);
            }
            else
            {
                Send(exchangeName, routingKey, requestMessage, correlationData);
                if (ShouldWaitForConfirm && correlationData != null)
                {
                    WaitForConfirm(requestMessage, correlationData);
                }

                return null;
            }
        }

        private void WaitForConfirm(IMessage requestMessage, CorrelationData correlationData)
        {
            try
            {
                if (!correlationData.Future.Wait(WaitForConfirmTimeout))
                {
                    throw new MessageTimeoutException(requestMessage, this + ": Timed out awaiting publisher confirm");
                }

                var confirm = correlationData.Future.Result;
                if (!confirm.Ack)
                {
                    throw new RabbitException("Negative publisher confirm received: " + confirm);
                }

                if (correlationData.ReturnedMessage != null)
                {
                    throw new RabbitException("Message was returned by the broker");
                }
            }
            catch (Exception e)
            {
                throw new RabbitException("Failed to get publisher confirm", e);
            }
        }

        private void Send(string exchangeName, string routingKey, IMessage requestMessage, CorrelationData correlationData)
        {
            var converter = Template.MessageConverter;

            var message = MappingUtils.MapMessage(requestMessage, converter, HeaderMapper, DefaultDeliveryMode, HeadersMappedLast);
            AddDelayProperty(message);
            Template.Send(exchangeName, routingKey, message, correlationData);
        }

        private IMessageBuilder SendAndReceive(string exchangeName, string routingKey, IMessage requestMessage, CorrelationData correlationData)
        {
            var converter = Template.MessageConverter;

            var message = MappingUtils.MapMessage(requestMessage, converter, HeaderMapper, DefaultDeliveryMode, HeadersMappedLast);
            AddDelayProperty(message);

            var amqpReplyMessage = Template.SendAndReceive(exchangeName, routingKey, message, correlationData);

            if (amqpReplyMessage == null)
            {
                return null;
            }

            return BuildReply(converter, amqpReplyMessage);
        }
    }
}

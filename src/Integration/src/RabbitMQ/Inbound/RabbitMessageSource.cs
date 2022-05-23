// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Acks;
using Steeltoe.Integration.Endpoint;
using Steeltoe.Integration.Rabbit.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.RabbitMQ.Batch;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Support;
using System;
using System.Collections.Generic;
using RabbitConverter = Steeltoe.Messaging.RabbitMQ.Support.Converter;
using RC = RabbitMQ.Client;

namespace Steeltoe.Integration.Rabbit.Inbound
{
    public class RabbitMessageSource : AbstractMessageSource<object>
    {
        public RabbitMessageSource(IApplicationContext context, IConnectionFactory connectionFactory, string queueName)
            : this(context, connectionFactory, new RabbitAckCallbackFactory(), queueName)
        {
        }

        public RabbitMessageSource(IApplicationContext context, IConnectionFactory connectionFactory, RabbitAckCallbackFactory ackCallbackFactory, string queueName)
            : base(context)
        {
            ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            AckCallbackFactory = ackCallbackFactory ?? throw new ArgumentNullException(nameof(ackCallbackFactory));
            QueueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
        }

        public IConnectionFactory ConnectionFactory { get; }

        public RabbitAckCallbackFactory AckCallbackFactory { get; }

        public string QueueName { get; }

        public bool Transacted { get; set; }

        public IMessageHeadersConverter MessageHeaderConverter { get; set; } = new DefaultMessageHeadersConverter();

        // public RabbitHeaderMapper HeaderMapper { get; set; }  = DefaultAmqpHeaderMapper.inboundMapper();
        public ISmartMessageConverter MessageConverter { get; set; } = new RabbitConverter.SimpleMessageConverter();

        public bool RawMessageHeader { get; set; }

        public IBatchingStrategy BatchingStrategy { get; set; } = new SimpleBatchingStrategy(0, 0, 0L);

        protected override object DoReceive()
        {
            var connection = ConnectionFactory.CreateConnection();
            var channel = connection.CreateChannel(Transacted);
            try
            {
                var resp = channel.BasicGet(QueueName, false);
                if (resp == null)
                {
                    RabbitUtils.CloseChannel(channel);
                    RabbitUtils.CloseConnection(connection);
                    return null;
                }

                var callback = AckCallbackFactory.CreateCallback(new RabbitAckInfo(connection, channel, Transacted, resp));
                var envelope = new Envelope(resp.DeliveryTag, resp.Redelivered, resp.Exchange, resp.RoutingKey);
                var messageProperties = MessageHeaderConverter.ToMessageHeaders(resp.BasicProperties, envelope, EncodingUtils.Utf8);
                var accessor = RabbitHeaderAccessor.GetMutableAccessor(messageProperties);
                accessor.ConsumerQueue = QueueName;

                // Map<String, Object> headers = this.headerMapper.toHeadersFromRequest(messageProperties);
                var message = Message.Create<byte[]>(resp.Body, accessor.MessageHeaders);

                object payload;
                if (BatchingStrategy.CanDebatch(message.Headers))
                {
                    var payloads = new List<object>();
                    BatchingStrategy.DeBatch(message, fragment => payloads.Add(MessageConverter.FromMessage(fragment, null)));
                    payload = payloads;
                }
                else
                {
                    payload = MessageConverter.FromMessage(message, null);
                }

                var builder = MessageBuilderFactory.WithPayload(payload)
                        .CopyHeaders(accessor.MessageHeaders)
                        .SetHeader(IntegrationMessageHeaderAccessor.ACKNOWLEDGMENT_CALLBACK, callback);
                if (RawMessageHeader)
                {
                    builder.SetHeader(RabbitMessageHeaderErrorMessageStrategy.AMQP_RAW_MESSAGE, message);
                    builder.SetHeader(IntegrationMessageHeaderAccessor.SOURCE_DATA, message);
                }

                return builder;
            }
            catch (Exception e)
            {
                RabbitUtils.CloseChannel(channel);
                RabbitUtils.CloseConnection(connection);
                throw RabbitExceptionTranslator.ConvertRabbitAccessException(e);
            }
        }

        public class RabbitAckCallbackFactory : IAcknowledgmentCallbackFactory<RabbitAckInfo>
        {
            public IAcknowledgmentCallback CreateCallback(RabbitAckInfo info)
            {
                return new RabbitAckCallback(info);
            }
        }

        public class RabbitAckInfo
        {
            public RabbitAckInfo(IConnection connection, RC.IModel channel, bool transacted, RC.BasicGetResult getResponse)
            {
                Connection = connection;
                Channel = channel;
                Transacted = transacted;
                Response = getResponse;
            }

            public IConnection Connection { get; }

            public RC.IModel Channel { get; }

            public bool Transacted { get; }

            public RC.BasicGetResult Response { get; }

            public override string ToString()
            {
                return $"RabbitAckInfo [connection={Connection}, channel={Channel}, transacted={Transacted}, getResponse={Response}]";
            }
        }

        public class RabbitAckCallback : IAcknowledgmentCallback
        {
            public RabbitAckCallback(RabbitAckInfo ackInfo)
            {
                AckInfo = ackInfo;
            }

            public RabbitAckInfo AckInfo { get; }

            public bool IsAcknowledged { get; set; }

            public bool IsAutoAck { get; set; } = true;

            public void Acknowledge(Status status)
            {
                // logger.trace("acknowledge(" + status + ") for " + this);
                try
                {
                    var deliveryTag = AckInfo.Response.DeliveryTag;
                    switch (status)
                    {
                        case Status.ACCEPT:
                            AckInfo.Channel.BasicAck(deliveryTag, false);
                            break;
                        case Status.REJECT:
                            AckInfo.Channel.BasicReject(deliveryTag, false);
                            break;
                        case Status.REQUEUE:
                            AckInfo.Channel.BasicReject(deliveryTag, true);
                            break;
                        default:
                            break;
                    }

                    if (AckInfo.Transacted)
                    {
                        AckInfo.Channel.TxCommit();
                    }
                }
                catch (Exception e)
                {
                    throw RabbitExceptionTranslator.ConvertRabbitAccessException(e);
                }
                finally
                {
                    RabbitUtils.CloseChannel(AckInfo.Channel);
                    RabbitUtils.CloseConnection(AckInfo.Connection);
                    IsAcknowledged = true;
                }
            }
        }
    }
}

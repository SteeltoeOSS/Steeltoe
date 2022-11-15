// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Acks;
using Steeltoe.Integration.Endpoint;
using Steeltoe.Integration.RabbitMQ.Support;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.RabbitMQ.Batch;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Support;
using RabbitConverter = Steeltoe.Messaging.RabbitMQ.Support.Converter;
using RC = RabbitMQ.Client;

namespace Steeltoe.Integration.RabbitMQ.Inbound;

public class RabbitMessageSource : AbstractMessageSource<object>
{
    public IConnectionFactory ConnectionFactory { get; }

    public RabbitAckCallbackFactory AckCallbackFactory { get; }

    public string QueueName { get; }

    public bool Transacted { get; set; }

    public IMessageHeadersConverter MessageHeaderConverter { get; set; } = new DefaultMessageHeadersConverter();

    // public RabbitHeaderMapper HeaderMapper { get; set; }  = DefaultAmqpHeaderMapper.inboundMapper();
    public ISmartMessageConverter MessageConverter { get; set; } = new RabbitConverter.SimpleMessageConverter();

    public bool RawMessageHeader { get; set; }

    public IBatchingStrategy BatchingStrategy { get; set; } = new SimpleBatchingStrategy(0, 0, 0L);

    public RabbitMessageSource(IApplicationContext context, IConnectionFactory connectionFactory, string queueName)
        : this(context, connectionFactory, new RabbitAckCallbackFactory(), queueName)
    {
    }

    public RabbitMessageSource(IApplicationContext context, IConnectionFactory connectionFactory, RabbitAckCallbackFactory ackCallbackFactory, string queueName)
        : base(context)
    {
        ArgumentGuard.NotNull(connectionFactory);
        ArgumentGuard.NotNull(ackCallbackFactory);
        ArgumentGuard.NotNull(queueName);

        ConnectionFactory = connectionFactory;
        AckCallbackFactory = ackCallbackFactory;
        QueueName = queueName;
    }

    protected override object DoReceive()
    {
        IConnection connection = ConnectionFactory.CreateConnection();
        RC.IModel channel = connection.CreateChannel(Transacted);

        try
        {
            RC.BasicGetResult resp = channel.BasicGet(QueueName, false);

            if (resp == null)
            {
                RabbitUtils.CloseChannel(channel);
                RabbitUtils.CloseConnection(connection);
                return null;
            }

            IAcknowledgmentCallback callback = AckCallbackFactory.CreateCallback(new RabbitAckInfo(connection, channel, Transacted, resp));
            var envelope = new Envelope(resp.DeliveryTag, resp.Redelivered, resp.Exchange, resp.RoutingKey);
            IMessageHeaders messageProperties = MessageHeaderConverter.ToMessageHeaders(resp.BasicProperties, envelope, EncodingUtils.Utf8);
            RabbitHeaderAccessor accessor = RabbitHeaderAccessor.GetMutableAccessor(messageProperties);
            accessor.ConsumerQueue = QueueName;

            // Map<String, Object> headers = this.headerMapper.toHeadersFromRequest(messageProperties);
            IMessage<byte[]> message = Message.Create(resp.Body, accessor.MessageHeaders);

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

            IMessageBuilder builder = MessageBuilderFactory.WithPayload(payload).CopyHeaders(accessor.MessageHeaders)
                .SetHeader(IntegrationMessageHeaderAccessor.AcknowledgmentCallback, callback);

            if (RawMessageHeader)
            {
                builder.SetHeader(RabbitMessageHeaderErrorMessageStrategy.AmqpRawMessage, message);
                builder.SetHeader(IntegrationMessageHeaderAccessor.SourceData, message);
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
        public IConnection Connection { get; }

        public RC.IModel Channel { get; }

        public bool Transacted { get; }

        public RC.BasicGetResult Response { get; }

        public RabbitAckInfo(IConnection connection, RC.IModel channel, bool transacted, RC.BasicGetResult getResponse)
        {
            Connection = connection;
            Channel = channel;
            Transacted = transacted;
            Response = getResponse;
        }

        public override string ToString()
        {
            return $"RabbitAckInfo [connection={Connection}, channel={Channel}, transacted={Transacted}, getResponse={Response}]";
        }
    }

    public class RabbitAckCallback : IAcknowledgmentCallback
    {
        public RabbitAckInfo AckInfo { get; }

        public bool IsAcknowledged { get; set; }

        public bool IsAutoAck { get; set; } = true;

        public RabbitAckCallback(RabbitAckInfo ackInfo)
        {
            AckInfo = ackInfo;
        }

        public void Acknowledge(Status status)
        {
            // logger.trace("acknowledge(" + status + ") for " + this);
            try
            {
                ulong deliveryTag = AckInfo.Response.DeliveryTag;

                switch (status)
                {
                    case Status.Accept:
                        AckInfo.Channel.BasicAck(deliveryTag, false);
                        break;
                    case Status.Reject:
                        AckInfo.Channel.BasicReject(deliveryTag, false);
                        break;
                    case Status.Requeue:
                        AckInfo.Channel.BasicReject(deliveryTag, true);
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

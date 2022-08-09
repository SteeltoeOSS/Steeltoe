// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.RabbitMQ.Listener.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Support;

namespace Steeltoe.Messaging.RabbitMQ.Batch;

public class SimpleBatchingStrategy : IBatchingStrategy
{
    private readonly int _batchSize;
    private readonly int _bufferLimit;
    private readonly long _timeout;
    private readonly List<IMessage<byte[]>> _messages = new();
    private readonly List<MessageBatch> _empty = new();

    private string _exchange;
    private string _routingKey;
    private int _currentSize;

    public SimpleBatchingStrategy(int batchSize, int bufferLimit, long timeout)
    {
        _batchSize = batchSize;
        _bufferLimit = bufferLimit;
        _timeout = timeout;
    }

    public MessageBatch? AddToBatch(string exchange, string routingKey, IMessage message)
    {
        if (_exchange != null && _exchange != exchange)
        {
            throw new ArgumentException("Cannot send to different exchanges in the same batch.", nameof(exchange));
        }

        _exchange = exchange;

        if (_routingKey != null && _routingKey != routingKey)
        {
            throw new ArgumentException("Cannot send with different routing keys in the same batch.", nameof(routingKey));
        }

        if (message is not IMessage<byte[]> bytesMessage)
        {
            throw new ArgumentException($"{nameof(SimpleBatchingStrategy)} only supports messages with byte[] payloads.", nameof(message));
        }

        _routingKey = routingKey;

        int bufferUse = 4 + bytesMessage.Payload.Length;
        MessageBatch? batch = null;

        if (_messages.Count > 0 && _currentSize + bufferUse > _bufferLimit)
        {
            batch = DoReleaseBatch();
            _exchange = exchange;
            _routingKey = routingKey;
        }

        _currentSize += bufferUse;
        _messages.Add(bytesMessage);

        if (batch == null && (_messages.Count >= _batchSize || _currentSize >= _bufferLimit))
        {
            batch = DoReleaseBatch();
        }

        return batch;
    }

    public DateTime? NextRelease()
    {
        if (_messages.Count == 0 || _timeout <= 0)
        {
            return null;
        }

        if (_currentSize >= _bufferLimit)
        {
            // release immediately, we're already over the limit
            return DateTime.Now;
        }

        return DateTime.Now + TimeSpan.FromMilliseconds(_timeout);
    }

    public ICollection<MessageBatch> ReleaseBatches()
    {
        MessageBatch? batch = DoReleaseBatch();

        if (batch == null)
        {
            return _empty;
        }

        return new List<MessageBatch>
        {
            batch.Value
        };
    }

    public bool CanDebatch(IMessageHeaders properties)
    {
        if (properties.TryGetValue(RabbitMessageHeaders.SpringBatchFormat, out object value))
        {
            return value as string == RabbitMessageHeaders.BatchFormatLengthHeader4;
        }

        return false;
    }

    public void DeBatch(IMessage message, Action<IMessage> fragmentConsumer)
    {
        if (message is not IMessage<byte[]> bytesMessage)
        {
            throw new ArgumentException($"{nameof(SimpleBatchingStrategy)} only supports messages with byte[] payloads.", nameof(message));
        }

        RabbitHeaderAccessor accessor = RabbitHeaderAccessor.GetMutableAccessor(bytesMessage);
        var byteBuffer = new Span<byte>(bytesMessage.Payload);
        accessor.RemoveHeader(RabbitMessageHeaders.SpringBatchFormat);
        int bodyLength = bytesMessage.Payload.Length;
        int index = 0;

        while (index < bodyLength)
        {
            accessor = RabbitHeaderAccessor.GetMutableAccessor(bytesMessage);
            Span<byte> slice = byteBuffer.Slice(index);
            int length = BinaryPrimitives.ReadInt32BigEndian(slice);
            index += 4;

            if (length < 0 || length > bodyLength - index)
            {
                throw new ListenerExecutionFailedException("Bad batched message received",
                    new MessageConversionException($"Insufficient batch data at offset {index}"), bytesMessage);
            }

            byte[] body = new byte[length];
            slice = byteBuffer.Slice(index);

            for (int i = 0; i < length; i++)
            {
                body[i] = slice[i];
            }

            index += length;

            accessor.ContentLength = length;

            // Caveat - shared MessageProperties.
            if (index >= bodyLength)
            {
                accessor.LastInBatch = true;
            }

            IMessage<byte[]> fragment = Message.Create(body, accessor.MessageHeaders);

            fragmentConsumer(fragment);
        }
    }

    private MessageBatch? DoReleaseBatch()
    {
        if (_messages.Count < 1)
        {
            return null;
        }

        IMessage<byte[]> message = AssembleMessage();
        var messageBatch = new MessageBatch(_exchange, _routingKey, message);
        _messages.Clear();
        _currentSize = 0;
        _exchange = null;
        _routingKey = null;
        return messageBatch;
    }

    private IMessage<byte[]> AssembleMessage()
    {
        if (_messages.Count == 1)
        {
            return _messages[0];
        }

        RabbitHeaderAccessor accessor = RabbitHeaderAccessor.GetMutableAccessor(_messages[0]);
        byte[] body = new byte[_currentSize];

        var bytes = new Span<byte>(body);
        int index = 0;

        foreach (IMessage<byte[]> message in _messages)
        {
            Span<byte> slice = bytes.Slice(index);
            BinaryPrimitives.WriteInt32BigEndian(slice, message.Payload.Length);
            index += 4;

            slice = bytes.Slice(index);

            for (int i = 0; i < message.Payload.Length; i++)
            {
                slice[i] = message.Payload[i];
            }

            index += message.Payload.Length;
        }

        accessor.SetHeader(RabbitMessageHeaders.SpringBatchFormat, RabbitMessageHeaders.BatchFormatLengthHeader4);
        accessor.SetHeader(RabbitMessageHeaders.BatchSize, _messages.Count);
        return Message.Create(body, accessor.MessageHeaders);
    }
}

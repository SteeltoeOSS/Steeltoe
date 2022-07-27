// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.RabbitMQ.Listener.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Support;
using Steeltoe.Messaging.Support;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Steeltoe.Messaging.RabbitMQ.Batch;

public class SimpleBatchingStrategy : IBatchingStrategy
{
    private readonly int _batchSize;
    private readonly int _bufferLimit;
    private readonly long _timeout;
    private readonly List<IMessage<byte[]>> _messages = new ();
    private readonly List<MessageBatch> _empty = new ();

    private string _exchange;
    private string _routingKey;
    private int _currentSize;

    public SimpleBatchingStrategy(int batchSize, int bufferLimit, long timeout)
    {
        _batchSize = batchSize;
        _bufferLimit = bufferLimit;
        _timeout = timeout;
    }

    public MessageBatch? AddToBatch(string exch, string routKey, IMessage input)
    {
        if (_exchange != null && _exchange != exch)
        {
            throw new ArgumentException("Cannot send to different exchanges in the same batch");
        }

        _exchange = exch;

        if (_routingKey != null && _routingKey != routKey)
        {
            throw new ArgumentException("Cannot send with different routing keys in the same batch");
        }

        if (input is not IMessage<byte[]> message)
        {
            throw new ArgumentException("SimpleBatchingStrategy only supports messages with byte[] payloads");
        }

        _routingKey = routKey;

        var bufferUse = 4 + message.Payload.Length;
        MessageBatch? batch = null;
        if (_messages.Count > 0 && _currentSize + bufferUse > _bufferLimit)
        {
            batch = DoReleaseBatch();
            _exchange = exch;
            _routingKey = routKey;
        }

        _currentSize += bufferUse;
        _messages.Add(message);
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
        else if (_currentSize >= _bufferLimit)
        {
            // release immediately, we're already over the limit
            return DateTime.Now;
        }
        else
        {
            return DateTime.Now + TimeSpan.FromMilliseconds(_timeout);
        }
    }

    public ICollection<MessageBatch> ReleaseBatches()
    {
        var batch = DoReleaseBatch();
        if (batch == null)
        {
            return _empty;
        }
        else
        {
            return new List<MessageBatch>() { batch.Value };
        }
    }

    public bool CanDebatch(IMessageHeaders properties)
    {
        if (properties.TryGetValue(RabbitMessageHeaders.SPRING_BATCH_FORMAT, out var value))
        {
            return (value as string) == RabbitMessageHeaders.BATCH_FORMAT_LENGTH_HEADER4;
        }

        return false;
    }

    public void DeBatch(IMessage input, Action<IMessage> fragmentConsumer)
    {
        if (input is not IMessage<byte[]> message)
        {
            throw new ArgumentException("SimpleBatchingStrategy only supports messages with byte[] payloads");
        }

        var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
        var byteBuffer = new Span<byte>(message.Payload);
        accessor.RemoveHeader(RabbitMessageHeaders.SPRING_BATCH_FORMAT);
        var bodyLength = message.Payload.Length;
        var index = 0;
        while (index < bodyLength)
        {
            accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            var slice = byteBuffer.Slice(index);
            var length = BinaryPrimitives.ReadInt32BigEndian(slice);
            index += 4;
            if (length < 0 || length > bodyLength - index)
            {
                throw new ListenerExecutionFailedException(
                    "Bad batched message received",
                    new MessageConversionException("Insufficient batch data at offset " + index),
                    message);
            }

            var body = new byte[length];
            slice = byteBuffer.Slice(index);
            for (var i = 0; i < length; i++)
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

            var fragment = Message.Create(body, accessor.MessageHeaders);

            fragmentConsumer(fragment);
        }
    }

    private MessageBatch? DoReleaseBatch()
    {
        if (_messages.Count < 1)
        {
            return null;
        }

        var message = AssembleMessage();
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

        var accessor = RabbitHeaderAccessor.GetMutableAccessor(_messages[0]);
        var body = new byte[_currentSize];

        var bytes = new Span<byte>(body);
        var index = 0;
        foreach (var message in _messages)
        {
            var slice = bytes.Slice(index);
            BinaryPrimitives.WriteInt32BigEndian(slice, message.Payload.Length);
            index += 4;

            slice = bytes.Slice(index);
            for (var i = 0; i < message.Payload.Length; i++)
            {
                slice[i] = message.Payload[i];
            }

            index += message.Payload.Length;
        }

        accessor.SetHeader(RabbitMessageHeaders.SPRING_BATCH_FORMAT, RabbitMessageHeaders.BATCH_FORMAT_LENGTH_HEADER4);
        accessor.SetHeader(RabbitMessageHeaders.BATCH_SIZE, _messages.Count);
        return Message.Create(body, accessor.MessageHeaders);
    }
}
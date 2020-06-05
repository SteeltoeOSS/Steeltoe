// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Rabbit.Data;
using Steeltoe.Messaging.Rabbit.Listener.Exceptions;
using Steeltoe.Messaging.Rabbit.Support;
using Steeltoe.Messaging.Rabbit.Support.Converter;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Batch
{
    public class SimpleBatchingStrategy : IBatchingStrategy
    {
        private readonly int _batchSize;
        private readonly int _bufferLimit;
        private readonly long _timeout;
        private readonly List<Message> _messages = new List<Message>();
        private readonly List<MessageBatch> _empty = new List<MessageBatch>();

        private string _exchange;
        private string _routingKey;
        private int _currentSize;

        public SimpleBatchingStrategy(int batchSize, int bufferLimit, long timeout)
        {
            _batchSize = batchSize;
            _bufferLimit = bufferLimit;
            _timeout = timeout;
        }

        public MessageBatch AddToBatch(string exch, string routKey, Message message)
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

            _routingKey = routKey;

            var bufferUse = 4 + message.Body.Length;
            MessageBatch batch = null;
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

        public DateTime NextRelease()
        {
            if (_messages.Count == 0 || _timeout <= 0)
            {
                return default;
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
                return new List<MessageBatch>() { batch };
            }
        }

        public bool CanDebatch(MessageProperties properties)
        {
            if (properties.Headers.TryGetValue(MessageProperties.SPRING_BATCH_FORMAT, out var value))
            {
                return (value as string) == MessageProperties.BATCH_FORMAT_LENGTH_HEADER4;
            }

            return false;
        }

        public void DeBatch(Message message, Action<Message> fragmentConsumer)
        {
            var byteBuffer = new Span<byte>(message.Body);
            var messageProperties = message.MessageProperties;
            messageProperties.Headers.Remove(MessageProperties.SPRING_BATCH_FORMAT);
            var bodyLength = message.Body.Length;
            var index = 0;
            while (index < bodyLength)
            {
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
                for (var i = 0; i < message.Body.Length; i++)
                {
                    message.Body[i] = slice[i];
                }

                index = index + length;

                messageProperties.ContentLength = length;

                // Caveat - shared MessageProperties.
                var fragment = new Message(body, messageProperties);
                if (index >= bodyLength)
                {
                    messageProperties.LastInBatch = true;
                }

                fragmentConsumer(fragment);
            }
        }

        private MessageBatch DoReleaseBatch()
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

        private Message AssembleMessage()
        {
            if (_messages.Count == 1)
            {
                return _messages[0];
            }

            var messageProperties = _messages[0].MessageProperties;
            var body = new byte[_currentSize];

            var bytes = new Span<byte>(body);
            var index = 0;
            foreach (var message in _messages)
            {
                var slice = bytes.Slice(index);
                BinaryPrimitives.WriteInt32BigEndian(slice, message.Body.Length);
                index += 4;

                slice = bytes.Slice(index);
                for (var i = 0; i < message.Body.Length; i++)
                {
                    slice[i] = message.Body[i];
                }

                index = index + message.Body.Length;
            }

            messageProperties.Headers[MessageProperties.SPRING_BATCH_FORMAT] = MessageProperties.BATCH_FORMAT_LENGTH_HEADER4;
            messageProperties.Headers[AmqpHeaders.BATCH_SIZE] = _messages.Count;
            return new Message(body, messageProperties);
        }
    }
}

// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Messaging.Rabbit.Data;
using System;

namespace Steeltoe.Messaging.Rabbit.Core
{
    public interface IAmqpTemplate
    {
        void Send(Message message);

        void Send(string routingKey, Message message);

        void Send(string exchange, string routingKey, Message message);

        void ConvertAndSend(object message);

        void ConvertAndSend(string routingKey, object message);

        void ConvertAndSend(string exchange, string routingKey, object message);

        void ConvertAndSend(object message, IMessagePostProcessor messagePostProcessor);

        void ConvertAndSend(string routingKey, object message, IMessagePostProcessor messagePostProcessor);

        void ConvertAndSend(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor);

        Message Receive();

        Message Receive(string queueName);

        Message Receive(int timeoutMillis);

        Message Receive(string queueName, int timeoutMillis);

        object ReceiveAndConvert();

        object ReceiveAndConvert(string queueName);

        object ReceiveAndConvert(int timeoutMillis);

        object ReceiveAndConvert(string queueName, int timeoutMillis);

        T ReceiveAndConvert<T>(Type type);

        T ReceiveAndConvert<T>(string queueName, Type type);

        T ReceiveAndConvert<T>(int timeoutMillis, Type type);

        T ReceiveAndConvert<T>(string queueName, int timeoutMillis, Type type);

        bool ReceiveAndReply<R, S>(Func<R, S> callback)
            where R : class
            where S : class;

        bool ReceiveAndReply<R, S>(string queueName, Func<R, S> callback)
            where R : class
            where S : class;

        bool ReceiveAndReply<R, S>(Func<R, S> callback, string replyExchange, string replyRoutingKey)
            where R : class
            where S : class;

        bool ReceiveAndReply<R, S>(string queueName, Func<R, S> callback, string replyExchange, string replyRoutingKey)
            where R : class
            where S : class;

        bool ReceiveAndReply<R, S>(Func<R, S> callback, Func<Message, S, Address> replyToAddressCallback)
            where R : class
            where S : class;

        bool ReceiveAndReply<R, S>(string queueName, Func<R, S> callback, Func<Message, S, Address> replyToAddressCallback)
            where R : class
            where S : class;

        Message SendAndReceive(Message message);

        Message SendAndReceive(string routingKey, Message message);

        Message SendAndReceive(string exchange, string routingKey, Message message);

        object ConvertSendAndReceive(object message);

        object ConvertSendAndReceive(string routingKey, object message);

        object ConvertSendAndReceive(string exchange, string routingKey, object message);

        object ConvertSendAndReceive(object message, IMessagePostProcessor messagePostProcessor);

        object ConvertSendAndReceive(string routingKey, object message, IMessagePostProcessor messagePostProcessor);

        object ConvertSendAndReceive(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor);

        T ConvertSendAndReceiveAsType<T>(object message, Type responseType);

        T ConvertSendAndReceiveAsType<T>(string routingKey, object message, Type responseType);

        T ConvertSendAndReceiveAsType<T>(string exchange, string routingKey, object message, Type responseType);

        T ConvertSendAndReceiveAsType<T>(object message, IMessagePostProcessor messagePostProcessor, Type responseType);

        T ConvertSendAndReceiveAsType<T>(string routingKey, object message, IMessagePostProcessor messagePostProcessor, Type responseType);

        T ConvertSendAndReceiveAsType<T>(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor, Type responseType);
    }
}

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

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Steeltoe.Common.Services;
using Steeltoe.Messaging.Rabbit.Connection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Rabbit.Core
{
    public interface IRabbitTemplate : IServiceNameAware
    {
        Connection.IConnectionFactory ConnectionFactory { get; }

        T Execute<T>(Func<IModel, T> channelCallback);

        T Invoke<T>(Func<IRabbitTemplate, T> operationsCallback)
        {
            return Invoke(operationsCallback, null, null);
        }

        T Invoke<T>(Func<IRabbitTemplate, T> operationsCallback, Action<object, BasicAckEventArgs> acks, Action<object, BasicNackEventArgs> nacks);

        bool WaitForConfirms(int timeout);

        void WaitForConfirmsOrDie(int timeout);

        void Send(string exchange, string routingKey, IMessage message);

        void Send(string exchange, string routingKey, IMessage message, CorrelationData correlationData);

        Task SendAsync(string exchange, string routingKey, IMessage message, CancellationToken cancellationToken = default);

        Task SendAsync(string exchange, string routingKey, IMessage message, CorrelationData correlationData, CancellationToken cancellationToken = default);

        void ConvertAndSend(string exchange, string routingKey, object message, CorrelationData correlationData);

        void ConvertAndSend(object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData);

        void ConvertAndSend(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData);

        void ConvertAndSend(string exchange, string routingKey, object message);

        void ConvertAndSend(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor);

        T ConvertSendAndReceive<T>(object message, CorrelationData correlationData);

        T ConvertSendAndReceive<T>(string exchange, string routingKey, object message, CorrelationData correlationData);

        T ConvertSendAndReceive<T>(object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData);

        T ConvertSendAndReceive<T>(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData);

        T ConvertSendAndReceive<T>(string exchange, string routingKey, object message);

        IMessage Receive(int timeoutMillis);

        IMessage Receive(string queueName, int timeoutMillis);

        T ReceiveAndConvert<T>(int timeoutMillis);

        T ReceiveAndConvert<T>(string queueName, int timeoutMillis);

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

        bool ReceiveAndReply<R, S>(Func<R, S> callback, Func<IMessage, S, Address> replyToAddressCallback)
            where R : class
            where S : class;

        bool ReceiveAndReply<R, S>(string queueName, Func<R, S> callback, Func<IMessage, S, Address> replyToAddressCallback)
            where R : class
            where S : class;

        IMessage SendAndReceive(string exchange, string routingKey, IMessage message);

        public interface IOperationsCallback<out T>
        {
            T DoInRabbit(IRabbitTemplate operations);
        }
    }
}

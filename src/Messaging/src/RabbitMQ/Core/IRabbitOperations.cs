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
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Data;
using System;

namespace Steeltoe.Messaging.Rabbit.Core
{
    public interface IRabbitOperations : IAmqpTemplate, ILifecycle
    {
        Connection.IConnectionFactory ConnectionFactory { get; }

        T Execute<T>(Func<IModel, T> channelCallback);

        T Invoke<T>(Func<IRabbitOperations, T> operationsCallback)
        {
            return Invoke(operationsCallback, null, null);
        }

        T Invoke<T>(Func<IRabbitOperations, T> operationsCallback, Action<object, BasicAckEventArgs> acks, Action<object, BasicNackEventArgs> nacks);

        bool WaitForConfirms(int timeout);

        void WaitForConfirmsOrDie(int timeout);

        void Send(string exchange, string routingKey, Message message, CorrelationData correlationData);

        void CorrelationConvertAndSend(object message, CorrelationData correlationData);

        void ConvertAndSend(string routingKey, object message, CorrelationData correlationData);

        void ConvertAndSend(string exchange, string routingKey, object message, CorrelationData correlationData);

        void ConvertAndSend(object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData);

        void ConvertAndSend(string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData);

        void ConvertAndSend(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData);

        object ConvertSendAndReceive(object message, CorrelationData correlationData);

        object ConvertSendAndReceive(string routingKey, object message, CorrelationData correlationData);

        object ConvertSendAndReceive(string exchange, string routingKey, object message, CorrelationData correlationData);

        object ConvertSendAndReceive(object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData);

        object ConvertSendAndReceive(string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData);

        object ConvertSendAndReceive(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData);

        T ConvertSendAndReceiveAsType<T>(object message, CorrelationData correlationData, Type responseType);

        T ConvertSendAndReceiveAsType<T>(string routingKey, object message, CorrelationData correlationData, Type responseType);

        T ConvertSendAndReceiveAsType<T>(string exchange, string routingKey, object message, CorrelationData correlationData, Type responseType);

        T ConvertSendAndReceiveAsType<T>(object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData, Type responseType);

        T ConvertSendAndReceiveAsType<T>(string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData, Type responseType);

        T ConvertSendAndReceiveAsType<T>(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData, Type responseType);

        // void Start()
        // {
        //    // No-op - implemented for backward compatibility
        // }

        // void Stop()
        // {
        //    // No-op - implemented for backward compatibility
        // }

        // bool IsRunning => false;
        public interface IOperationsCallback<out T>
        {
            T DoInRabbit(IRabbitOperations operations);
        }
    }
}

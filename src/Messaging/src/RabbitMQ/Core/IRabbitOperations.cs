// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

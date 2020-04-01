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

using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Messaging.Rabbit.Data;
using Steeltoe.Messaging.Rabbit.Listener.Exceptions;
using Steeltoe.Messaging.Rabbit.Support;
using Steeltoe.Messaging.Rabbit.Support.Converter;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Messaging.Rabbit.Listener.Adapters
{
    public class MessagingMessageListenerAdapter : AbstractMessageListenerAdapter
    {
        public MessagingMessageListenerAdapter()
            : this(null, null)
        {
        }

        public MessagingMessageListenerAdapter(object instance, MethodInfo method)
            : this(instance, method, false, null)
        {
        }

        public MessagingMessageListenerAdapter(object instance, MethodInfo method, bool returnExceptions, IRabbitListenerErrorHandler errorHandler)
            : this(instance, method, returnExceptions, errorHandler, false)
        {
        }

        protected MessagingMessageListenerAdapter(object instance, MethodInfo method, bool returnExceptions, IRabbitListenerErrorHandler errorHandler, bool batch)
        {
            MessagingMessageConverter = new MessagingMessageConverterAdapter(this, instance, method, batch);
            ReturnExceptions = returnExceptions;
            ErrorHandler = errorHandler;
        }

        public bool ReturnExceptions { get; }

        public IRabbitListenerErrorHandler ErrorHandler { get; }

        public HandlerAdapter HandlerAdapter { get; set; }

        public IHeaderMapper<MessageProperties> HeaderMapper { get; set; }

        public override IMessageConverter MessageConverter
        {
            get
            {
                return base.MessageConverter;
            }

            set
            {
                base.MessageConverter = value;
                MessagingMessageConverter.PayloadConverter = value;
            }
        }

        public override void OnMessage(Message amqpMessage, IModel channel)
        {
            var message = ToMessagingMessage(amqpMessage);
            InvokeHandlerAndProcessResult(amqpMessage, channel, message);
        }

        protected MessagingMessageConverterAdapter MessagingMessageConverter { get; }

        protected void InvokeHandlerAndProcessResult(Message amqpMessage, IModel channel, IMessage message)
        {
            _logger?.LogDebug("Processing [" + message + "]");
            InvocationResult result = null;
            try
            {
                if (MessagingMessageConverter.Method == null)
                {
                    amqpMessage.MessageProperties.TargetMethod = HandlerAdapter.GetMethodFor(message.Payload);
                }

                result = InvokeHandler(amqpMessage, channel, message);
                if (result.ReturnValue != null)
                {
                    HandleResult(result, amqpMessage, channel, message);
                }
                else
                {
                    _logger?.LogTrace("No result object given - no result to handle");
                }
            }
            catch (ListenerExecutionFailedException e)
            {
                if (ErrorHandler != null)
                {
                    try
                    {
                        var messageWithChannel = Messaging.Support.MessageBuilder.FromMessage(message).SetHeader(AmqpHeaders.CHANNEL, channel).Build();
                        var errorResult = ErrorHandler.HandleError(amqpMessage, messageWithChannel, e);
                        if (errorResult != null)
                        {
                            HandleResult(HandlerAdapter.GetInvocationResultFor(errorResult, message.Payload), amqpMessage, channel, message);
                        }
                        else
                        {
                            _logger?.LogTrace("Error handler returned no result");
                        }
                    }
                    catch (Exception ex)
                    {
                        ReturnOrThrow(amqpMessage, channel, message, ex, ex);
                    }
                }
                else
                {
                    ReturnOrThrow(amqpMessage, channel, message, e.InnerException, e);
                }
            }
        }

        protected virtual IMessage ToMessagingMessage(Message amqpMessage)
        {
            return (IMessage)MessagingMessageConverter.FromMessage(amqpMessage);
        }

        protected override Message BuildMessage(IModel channel, object result, Type genericType)
        {
            var converter = MessageConverter;
            if (converter != null && !(result is Message))
            {
                if (result is IMessage)
                {
                    return MessagingMessageConverter.ToMessage(result, new MessageProperties());
                }
                else
                {
                    return converter.ToMessage(result, new MessageProperties(), genericType);
                }
            }
            else
            {
                if (!(result is Message))
                {
                    throw new MessageConversionException("No MessageConverter specified - cannot handle message [" + result + "]");
                }

                return (Message)result;
            }
        }

        private InvocationResult InvokeHandler(Message amqpMessage, IModel channel, IMessage message)
        {
            try
            {
                return HandlerAdapter.Invoke(message, amqpMessage, channel);
            }
            catch (MessagingException ex)
            {
                throw new ListenerExecutionFailedException(
                    CreateMessagingErrorMessage("Listener method could not be invoked with the incoming message", message.Payload), ex, amqpMessage);
            }
            catch (Exception ex)
            {
                throw new ListenerExecutionFailedException("Listener method '" + HandlerAdapter.GetMethodAsString(message.Payload) + "' threw exception", ex, amqpMessage);
            }
        }

        private string CreateMessagingErrorMessage(string description, object payload)
        {
            return description + "\n"
                    + "Endpoint handler details:\n"
                    + "Method [" + HandlerAdapter.GetMethodAsString(payload) + "]\n"
                    + "Bean [" + HandlerAdapter.Instance + "]";
        }

        private void ReturnOrThrow(Message amqpMessage, IModel channel, IMessage message, Exception exceptionToRetrun, Exception exceptionToThrow)
        {
            if (!ReturnExceptions)
            {
                throw exceptionToThrow;
            }

            try
            {
                HandleResult(
                    new InvocationResult(
                    exceptionToRetrun,
                    null,
                    HandlerAdapter.GetReturnTypeFor(message.Payload),
                    HandlerAdapter.Instance,
                    HandlerAdapter.GetMethodFor(message.Payload)),
                    amqpMessage,
                    channel,
                    message);
            }
            catch (ReplyFailureException rfe)
            {
                if (typeof(void).Equals(HandlerAdapter.GetReturnTypeFor(message.Payload)))
                {
                    throw exceptionToThrow;
                }
                else
                {
                    throw;
                }
            }
        }

        protected class MessagingMessageConverterAdapter : MessagingMessageConverter
        {
            private readonly ILogger _logger;
            private readonly MessagingMessageListenerAdapter _adapter;

            public MessagingMessageConverterAdapter(MessagingMessageListenerAdapter adapter, object instance, MethodInfo method, bool batch, ILogger logger = null)
            {
                _logger = logger;
                _adapter = adapter;
                Instance = instance;
                Method = method;
                IsBatch = batch;
                InferredArgumentType = DetermineInferredType();
                if (InferredArgumentType != null)
                {
                    _logger?.LogDebug("Inferred argument type for " + method.ToString() + " is " + InferredArgumentType);
                }
            }

            public object Instance { get; }

            public MethodInfo Method { get; }

            public bool IsBatch { get; }

            public Type InferredArgumentType { get; }

            public bool IsMessageList { get; set; }

            public bool IsAmqpMessageList { get; set; }

            public override object ExtractPayload(Message message)
            {
                var messageProperties = message.MessageProperties;
                if (Instance != null)
                {
                    messageProperties.Target = Instance;
                }

                if (Method != null)
                {
                    messageProperties.TargetMethod = Method;
                    if (InferredArgumentType != null)
                    {
                        messageProperties.InferredArgumentType = InferredArgumentType;
                    }
                }

                return _adapter.ExtractMessage(message);
            }

            private Type DetermineInferredType()
            {
                if (Method == null)
                {
                    return null;
                }

                Type genericParameterType = null;

                foreach (var methodParameter in Method.GetParameters())
                {
                    /*
                     * We're looking for a single non-annotated parameter, or one annotated with @Payload.
                     * We ignore parameters with type Message because they are not involved with conversion.
                     */
                    if (IsEligibleParameter(methodParameter)
                            && (methodParameter.GetCustomAttributes(false).Length == 0
                            || methodParameter.GetCustomAttribute(typeof(PayloadAttribute)) != null))
                    {
                        if (genericParameterType == null)
                        {
                            genericParameterType = ExtractGenericParameterTypFromMethodParameter(methodParameter);
                        }
                        else
                        {
                            _logger?.LogDebug("Ambiguous parameters for target payload for method " + Method + "; no inferred type header added");
                            return null;
                        }
                    }
                }

                return genericParameterType;
            }

            // Don't consider parameter types that are available after conversion.
            // Message, Message<?> and Channel.
            private bool IsEligibleParameter(ParameterInfo methodParameter)
            {
                var parameterType = methodParameter.ParameterType;
                if (parameterType.Equals(typeof(IModel))
                    || parameterType.Equals(typeof(Message)))
                {
                    return false;
                }

                if (parameterType.IsGenericType)
                {
                    var typeDef = parameterType.GetGenericTypeDefinition();
                    if (typeDef.Equals(typeof(IMessage<>)))
                    {
                        return false;
                    }
                }

                return !parameterType.Equals(typeof(IMessage)); // could be Message without a generic type
            }

            private Type ExtractGenericParameterTypFromMethodParameter(ParameterInfo methodParameter)
            {
                var parameterType = methodParameter.ParameterType;
                if (parameterType.IsGenericType)
                {
                    var typeDef = parameterType.GetGenericTypeDefinition();
                    if (typeDef.Equals(typeof(IMessage<>)))
                    {
                        parameterType = typeDef.GetGenericArguments()[0];
                    }
                    else if (IsBatch && typeDef.Equals(typeof(List<>)))
                    {
                        var paramType = typeDef.GetGenericArguments()[0];
                        var messageHasGeneric = paramType.IsGenericType && paramType.GetGenericTypeDefinition().Equals(typeof(IMessage<>));
                        IsMessageList = paramType.Equals(typeof(IMessage)) || messageHasGeneric;
                        IsAmqpMessageList = paramType.Equals(typeof(Message));
                        if (messageHasGeneric)
                        {
                            parameterType = paramType.GetGenericArguments()[0];
                        }
                        else
                        {
                            // when decoding batch messages we convert to the List's generic type
                            parameterType = paramType;
                        }
                    }
                }

                return parameterType;
            }
        }
    }
}

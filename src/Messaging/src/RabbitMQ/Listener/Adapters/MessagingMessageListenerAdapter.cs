// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Messaging.Rabbit.Listener.Exceptions;
using Steeltoe.Messaging.Rabbit.Support;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Messaging.Rabbit.Listener.Adapters
{
    public class MessagingMessageListenerAdapter : AbstractMessageListenerAdapter
    {
        public MessagingMessageListenerAdapter(IApplicationContext context, ILogger logger = null)
            : this(context, null, null, logger)
        {
        }

        public MessagingMessageListenerAdapter(IApplicationContext context, object instance, MethodInfo method, ILogger logger = null)
            : this(context, instance, method, false, null, logger)
        {
        }

        public MessagingMessageListenerAdapter(IApplicationContext context, object instance, MethodInfo method, bool returnExceptions, IRabbitListenerErrorHandler errorHandler, ILogger logger = null)
            : this(context, instance, method, returnExceptions, errorHandler, false, logger)
        {
        }

        protected MessagingMessageListenerAdapter(IApplicationContext context, object instance, MethodInfo method, bool returnExceptions, IRabbitListenerErrorHandler errorHandler, bool batch, ILogger logger = null)
            : base(context, logger)
        {
            Instance = instance;
            Method = method;
            IsBatch = batch;
            ReturnExceptions = returnExceptions;
            ErrorHandler = errorHandler;
            InferredArgumentType = DetermineInferredType();
        }

        public virtual object Instance { get; }

        public virtual MethodInfo Method { get; }

        public bool IsBatch { get; }

        public bool IsMessageList { get; set; }

        public bool IsMessageByteArrayList { get; set; }

        public Type InferredArgumentType { get; set; }

        public bool ReturnExceptions { get; }

        public IRabbitListenerErrorHandler ErrorHandler { get; }

        public HandlerAdapter HandlerAdapter { get; set; }

        public override void OnMessage(IMessage amqpMessage, IModel channel)
        {
            PreprocesMessage(amqpMessage);
            var headers = amqpMessage.Headers;
            var convertedObject = MessageConverter.FromMessage(amqpMessage, InferredArgumentType);
            if (convertedObject == null)
            {
                throw new MessageConversionException("Message converter returned null");
            }

            var builder = (convertedObject is IMessage) ? RabbitMessageBuilder.FromMessage((IMessage)convertedObject) : RabbitMessageBuilder.WithPayload(convertedObject);
            var message = builder.CopyHeadersIfAbsent(headers).Build();
            InvokeHandlerAndProcessResult(amqpMessage, channel, message);
        }

        protected internal override IMessage<byte[]> BuildMessage(IModel channel, object result, Type genericType)
        {
            var converter = MessageConverter;
            if (converter != null)
            {
                if (result is IMessage)
                {
                    var asMessage = result as IMessage;
                    result = converter.ToMessage(asMessage.Payload, asMessage.Headers, genericType);
                }
                else
                {
                    result = converter.ToMessage(result, new MessageHeaders(), genericType);
                }
            }

            if (!(result is IMessage<byte[]>))
            {
                throw new MessageConversionException("No MessageConverter specified - cannot handle message [" + result + "]");
            }

            return (IMessage<byte[]>)result;
        }

        protected void InvokeHandlerAndProcessResult(IMessage amqpMessage, IModel channel, IMessage message)
        {
            _logger?.LogDebug("Processing [{message}]", message);
            InvocationResult result = null;
            try
            {
                if (this.Method == null)
                {
                    var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
                    accessor.TargetMethod = HandlerAdapter.GetMethodFor(message.Payload);
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
                        var messageWithChannel = RabbitMessageBuilder.FromMessage(message).SetHeader(RabbitMessageHeaders.CHANNEL, channel).Build();
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

        protected void PreprocesMessage(IMessage message)
        {
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            if (Instance != null)
            {
                accessor.Target = Instance;
            }

            if (Method != null)
            {
                accessor.TargetMethod = Method;
                if (InferredArgumentType != null)
                {
                    accessor.InferredArgumentType = InferredArgumentType;
                }
            }
        }

        private InvocationResult InvokeHandler(IMessage amqpMessage, IModel channel, IMessage message)
        {
            try
            {
                return HandlerAdapter.Invoke(message, channel);
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

        private void ReturnOrThrow(IMessage amqpMessage, IModel channel, IMessage message, Exception exceptionToRetrun, Exception exceptionToThrow)
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
            catch (ReplyFailureException)
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
                        _logger?.LogDebug("Ambiguous parameters for target payload for method {method}; no inferred type header added", Method);
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
            if (parameterType.Equals(typeof(IModel)))
            {
                return false;
            }

            if (parameterType.IsGenericType)
            {
                var typeDef = parameterType.GetGenericTypeDefinition();
                if (typeDef.Equals(typeof(IMessage<>)))
                {
                    return true;
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
                    parameterType = parameterType.GetGenericArguments()[0];
                }
                else if (IsBatch && typeDef.Equals(typeof(List<>)))
                {
                    var paramType = parameterType.GetGenericArguments()[0];
                    var messageHasGeneric = paramType.IsGenericType && paramType.GetGenericTypeDefinition().Equals(typeof(IMessage<>));
                    IsMessageList = paramType.Equals(typeof(IMessage)) || messageHasGeneric;
                    IsMessageByteArrayList = paramType.Equals(typeof(IMessage<byte[]>));
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Messaging.RabbitMQ.Listener.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Support;
using System;
using System.Collections.Generic;
using System.Reflection;
using RC=RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Listener.Adapters;

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

    public override void OnMessage(IMessage amqpMessage, RC.IModel channel)
    {
        PreProcessMessage(amqpMessage);
        var headers = amqpMessage.Headers;
        var convertedObject = MessageConverter.FromMessage(amqpMessage, InferredArgumentType);
        if (convertedObject == null)
        {
            throw new MessageConversionException("Message converter returned null");
        }

        var builder = convertedObject is IMessage message1 ? RabbitMessageBuilder.FromMessage(message1) : RabbitMessageBuilder.WithPayload(convertedObject);
        var message = builder.CopyHeadersIfAbsent(headers).Build();
        InvokeHandlerAndProcessResult(amqpMessage, channel, message);
    }

    protected internal override IMessage<byte[]> BuildMessage(RC.IModel channel, object result, Type genericType)
    {
        var converter = MessageConverter;
        if (converter != null)
        {
            if (result is IMessage asMessage)
            {
                result = converter.ToMessage(asMessage.Payload, asMessage.Headers, genericType);
            }
            else
            {
                result = converter.ToMessage(result, new MessageHeaders(), genericType);
            }
        }

        if (result is not IMessage<byte[]> mResult)
        {
            throw new MessageConversionException($"No MessageConverter specified - cannot handle message [{result}]");
        }

        return mResult;
    }

    protected void InvokeHandlerAndProcessResult(IMessage amqpMessage, RC.IModel channel, IMessage message)
    {
        Logger?.LogDebug("Processing [{message}]", message);
        InvocationResult result = null;
        try
        {
            if (Method == null)
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
                Logger?.LogTrace("No result object given - no result to handle");
            }
        }
        catch (ListenerExecutionFailedException e)
        {
            if (ErrorHandler != null)
            {
                try
                {
                    var messageWithChannel = RabbitMessageBuilder.FromMessage(message).SetHeader(RabbitMessageHeaders.Channel, channel).Build();
                    var errorResult = ErrorHandler.HandleError(amqpMessage, messageWithChannel, e);
                    if (errorResult != null)
                    {
                        HandleResult(HandlerAdapter.GetInvocationResultFor(errorResult, message.Payload), amqpMessage, channel, message);
                    }
                    else
                    {
                        Logger?.LogTrace("Error handler returned no result");
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

    protected void PreProcessMessage(IMessage message)
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

    private InvocationResult InvokeHandler(IMessage amqpMessage, RC.IModel channel, IMessage message)
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
            throw new ListenerExecutionFailedException($"Listener method '{HandlerAdapter.GetMethodAsString(message.Payload)}' threw exception", ex, amqpMessage);
        }
    }

    private string CreateMessagingErrorMessage(string description, object payload)
    {
        return $"{description}\nEndpoint handler details:\nMethod [{HandlerAdapter.GetMethodAsString(payload)}]\nBean [{HandlerAdapter.Instance}]";
    }

    private void ReturnOrThrow(IMessage amqpMessage, RC.IModel channel, IMessage message, Exception exceptionToReturn, Exception exceptionToThrow)
    {
        if (!ReturnExceptions)
        {
            throw exceptionToThrow;
        }

        try
        {
            HandleResult(
                new InvocationResult(
                    exceptionToReturn,
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
                    Logger?.LogDebug("Ambiguous parameters for target payload for method {method}; no inferred type header added", Method);
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
        if (parameterType.Equals(typeof(RC.IModel)))
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

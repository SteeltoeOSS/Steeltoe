// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Common.Expression.Internal.Contexts;
using Steeltoe.Common.Expression.Internal.Spring.Common;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Steeltoe.Common.Retry;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener.Support;
using Steeltoe.Messaging.RabbitMQ.Support;
using RC = RabbitMQ.Client;
using SimpleMessageConverter = Steeltoe.Messaging.RabbitMQ.Support.Converter.SimpleMessageConverter;

namespace Steeltoe.Messaging.RabbitMQ.Listener.Adapters;

public abstract class AbstractMessageListenerAdapter : IChannelAwareMessageListener
{
    private const string DefaultEncoding = "UTF-8";

    private static readonly SpelExpressionParser Parser = new();
    private static readonly IParserContext ParserContext = new TemplateParserContext("!{", "}");
    protected readonly ILogger Logger;

    public IApplicationContext ApplicationContext { get; set; }

    public virtual string Encoding { get; set; } = DefaultEncoding;

    public virtual string ResponseRoutingKey { get; set; } = string.Empty;

    public virtual string ResponseExchange { get; set; }

    public virtual Address ResponseAddress { get; set; }

    public virtual bool MandatoryPublish { get; set; }

    public virtual ISmartMessageConverter MessageConverter { get; set; }

#pragma warning disable S3956 // "Generic.List" instances should not be part of public APIs
    public virtual List<IMessagePostProcessor> BeforeSendReplyPostProcessors { get; private set; }
#pragma warning restore S3956 // "Generic.List" instances should not be part of public APIs

    public virtual RetryTemplate RetryTemplate { get; set; }

    public virtual IRecoveryCallback RecoveryCallback { get; set; }

    public virtual bool DefaultRequeueRejected { get; set; } = true;

    public virtual AcknowledgeMode ContainerAckMode { get; set; }

    public virtual bool IsManualAck => ContainerAckMode == AcknowledgeMode.Manual;

    public virtual StandardEvaluationContext EvalContext { get; set; } = new();

    public virtual IMessageHeadersConverter MessagePropertiesConverter { get; set; } = new DefaultMessageHeadersConverter();

    public virtual IExpression ResponseExpression { get; set; }

    public virtual IReplyPostProcessor ReplyPostProcessor { get; set; }

    protected AbstractMessageListenerAdapter(IApplicationContext context, ILogger logger = null)
    {
        Logger = logger;
        MessageConverter = new SimpleMessageConverter();
        ApplicationContext = context;
    }

    public virtual void SetResponseAddress(string defaultReplyTo)
    {
        if (defaultReplyTo.StartsWith(ParserContext.ExpressionPrefix))
        {
            ResponseExpression = Parser.ParseExpression(defaultReplyTo, ParserContext);
        }
        else
        {
            ResponseAddress = new Address(defaultReplyTo);
        }
    }

    public void SetServiceResolver(IServiceResolver serviceResolver)
    {
        EvalContext.ServiceResolver = serviceResolver;
        EvalContext.TypeConverter = new StandardTypeConverter();
        EvalContext.AddPropertyAccessor(new DictionaryAccessor());
    }

    public virtual void SetBeforeSendReplyPostProcessors(params IMessagePostProcessor[] beforeSendReplyPostProcessors)
    {
        ArgumentGuard.NotNull(beforeSendReplyPostProcessors);
        ArgumentGuard.ElementsNotNull(beforeSendReplyPostProcessors);

        BeforeSendReplyPostProcessors = new List<IMessagePostProcessor>(beforeSendReplyPostProcessors);
    }

    public abstract void OnMessage(IMessage message, RC.IModel channel);

    public virtual void OnMessage(IMessage message)
    {
        throw new InvalidOperationException("Should never be called for a ChannelAwareMessageListener");
    }

    public virtual void OnMessageBatch(IEnumerable<IMessage> messages, RC.IModel channel)
    {
        throw new NotSupportedException("This listener does not support message batches");
    }

    public virtual void OnMessageBatch(IEnumerable<IMessage> messages)
    {
        throw new NotSupportedException("This listener does not support message batches");
    }

    protected internal virtual IMessage<byte[]> BuildMessage(RC.IModel channel, object result, Type genericType)
    {
        ISmartMessageConverter converter = MessageConverter;

        if (converter != null && result is not IMessage<byte[]>)
        {
            result = converter.ToMessage(result, new MessageHeaders(), genericType);
        }

        if (result is not IMessage<byte[]> byteArrayMessage)
        {
            throw new MessageConversionException($"No MessageConverter specified - cannot handle message [{result}]");
        }

        return byteArrayMessage;
    }

    protected virtual void HandleListenerException(Exception exception)
    {
        Logger?.LogError(exception, "Listener execution failed");
    }

    protected virtual object ExtractMessage(IMessage message)
    {
        ISmartMessageConverter converter = MessageConverter;

        if (converter != null)
        {
            return converter.FromMessage(message, null);
        }

        return message;
    }

    protected virtual void HandleResult(InvocationResult resultArg, IMessage request, RC.IModel channel)
    {
        HandleResult(resultArg, request, channel, null);
    }

    protected virtual void HandleResult(InvocationResult resultArg, IMessage request, RC.IModel channel, object source)
    {
        if (channel != null)
        {
            if (resultArg.ReturnValue is Task asTask)
            {
                if (!IsManualAck)
                {
                    Logger?.LogWarning("Container AcknowledgeMode must be MANUAL for a Future<?> return type; " +
                        "otherwise the container will ack the message immediately");
                }

                asTask.ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        AsyncSuccess(resultArg, request, channel, source, GetReturnValue(t));
                        BasicAck(request, channel);
                    }
                    else
                    {
                        AsyncFailure(request, channel, t.Exception);
                    }
                });
            }
            else
            {
                DoHandleResult(resultArg, request, channel, source);
            }
        }
        else
        {
            Logger?.LogWarning("Listener method returned result [{result}]: not generating response message for it because no Rabbit Channel given", resultArg);
        }
    }

    protected virtual void DoHandleResult(InvocationResult resultArg, IMessage request, RC.IModel channel, object source)
    {
        Logger?.LogDebug("Listener method returned result [{result}] - generating response message for it", resultArg);

        try
        {
            IMessage<byte[]> response = BuildMessage(channel, resultArg.ReturnValue, resultArg.ReturnType);
            RabbitHeaderAccessor accessor = RabbitHeaderAccessor.GetMutableAccessor(response);
            accessor.Target = resultArg.Instance;
            accessor.TargetMethod = resultArg.Method;
            PostProcessResponse(request, response);
            Address replyTo = GetReplyToAddress(request, source, resultArg);
            SendResponse(channel, replyTo, response);
        }
        catch (Exception ex)
        {
            throw new ReplyFailureException($"Failed to send reply with payload '{resultArg}'", ex);
        }
    }

    protected virtual string GetReceivedExchange(IMessage request)
    {
        return request.Headers.ReceivedExchange();
    }

    protected virtual void PostProcessResponse(IMessage request, IMessage response)
    {
        string correlation = request.Headers.CorrelationId();

        if (correlation == null)
        {
            string messageId = request.Headers.MessageId();

            if (messageId != null)
            {
                correlation = messageId;
            }
        }

        RabbitHeaderAccessor accessor = RabbitHeaderAccessor.GetMutableAccessor(response);
        accessor.CorrelationId = correlation;
    }

    protected virtual Address GetReplyToAddress(IMessage request, object source, InvocationResult result)
    {
        Address replyTo = request.Headers.ReplyToAddress();

        if (replyTo == null)
        {
            if (ResponseAddress == null && ResponseExchange != null)
            {
                ResponseAddress = new Address(ResponseExchange, ResponseRoutingKey);
            }

            if (result.SendTo != null)
            {
                replyTo = EvaluateReplyTo(request, source, result.ReturnValue, result.SendTo);
            }
            else if (request.Headers.ReplyTo() != null)
            {
                return new Address(request.Headers.ReplyTo());
            }
            else if (ResponseExpression != null)
            {
                replyTo = EvaluateReplyTo(request, source, result.ReturnValue, ResponseExpression);
            }
            else if (ResponseAddress == null)
            {
                throw new RabbitException("Cannot determine ReplyTo message property value: " + "Request message does not contain reply-to property, " +
                    "and no default response Exchange was set.");
            }
            else
            {
                replyTo = ResponseAddress;
            }
        }

        return replyTo;
    }

    protected void SendResponse(RC.IModel channel, Address replyTo, IMessage<byte[]> messageIn)
    {
        IMessage<byte[]> message = messageIn;

        if (BeforeSendReplyPostProcessors != null)
        {
            List<IMessagePostProcessor> processors = BeforeSendReplyPostProcessors;
            IMessage postProcessed = message;

            foreach (IMessagePostProcessor postProcessor in processors)
            {
                postProcessed = postProcessor.PostProcessMessage(postProcessed);
            }

            message = postProcessed as IMessage<byte[]>;

            if (message == null)
            {
                throw new InvalidOperationException("A BeforeSendReplyPostProcessors failed to return IMessage<byte[]>");
            }
        }

        PostProcessChannel(channel, message);

        try
        {
            Logger?.LogDebug("Publishing response to exchange = [{exchange}], routingKey = [{routingKey}]", replyTo.ExchangeName, replyTo.RoutingKey);

            if (RetryTemplate == null)
            {
                DoPublish(channel, replyTo, message);
            }
            else
            {
                IMessage<byte[]> messageToSend = message;

                RetryTemplate.Execute<object>(_ =>
                {
                    DoPublish(channel, replyTo, messageToSend);
                    return null;
                }, ctx =>
                {
                    if (RecoveryCallback != null)
                    {
                        ctx.SetAttribute(SendRetryContextAccessor.Message, messageToSend);
                        ctx.SetAttribute(SendRetryContextAccessor.Address, replyTo);
                        RecoveryCallback.Recover(ctx);
                        return null;
                    }

                    throw RabbitExceptionTranslator.ConvertRabbitAccessException(ctx.LastException);
                });
            }
        }
        catch (Exception ex)
        {
            throw RabbitExceptionTranslator.ConvertRabbitAccessException(ex);
        }
    }

    protected virtual void DoPublish(RC.IModel channel, Address replyTo, IMessage<byte[]> message)
    {
        RC.IBasicProperties props = channel.CreateBasicProperties();
        MessagePropertiesConverter.FromMessageHeaders(message.Headers, props, EncodingUtils.GetEncoding(Encoding));
        channel.BasicPublish(replyTo.ExchangeName, replyTo.RoutingKey, MandatoryPublish, props, message.Payload);
    }

    protected virtual void PostProcessChannel(RC.IModel channel, IMessage response)
    {
    }

    private static object GetReturnValue(Task task)
    {
        Type taskType = task.GetType();

        if (!taskType.IsGenericType)
        {
            return null;
        }

        PropertyInfo property = taskType.GetProperty("Result");
        return property.GetValue(task);
    }

    private Address EvaluateReplyTo(IMessage request, object source, object result, IExpression expression)
    {
        object value = expression.GetValue(EvalContext, new ReplyExpressionRoot(request, source, result));

        Address replyTo = value switch
        {
            not string and not Address => throw new InvalidOperationException($"Response expression must be of type {nameof(String)} or {nameof(Address)}."),
            string stringValue => new Address(stringValue),
            _ => (Address)value
        };

        return replyTo;
    }

    private void AsyncSuccess(InvocationResult resultArg, IMessage request, RC.IModel channel, object source, object deferredResult)
    {
        if (deferredResult == null)
        {
            Logger?.LogDebug("Async result is null, ignoring");
        }
        else
        {
            Type returnType = resultArg.ReturnType;

            if (returnType != null)
            {
                Type[] actualTypeArguments = returnType.ContainsGenericParameters ? returnType.GetGenericArguments() : Array.Empty<Type>();

                if (actualTypeArguments.Length > 0)
                {
                    returnType = actualTypeArguments[0];

                    // if (returnType instanceof WildcardType)
                    //  {
                    //  // Set the return type to null so the converter will use the actual returned
                    //  // object's class for type info
                    //  returnType = null;
                    // }
                }
            }

            DoHandleResult(new InvocationResult(deferredResult, resultArg.SendTo, returnType, resultArg.Instance, resultArg.Method), request, channel, source);
        }
    }

    private void BasicAck(IMessage request, RC.IModel channel)
    {
        ulong? tag = request.Headers.DeliveryTag();
        ulong deliveryTag = tag ?? 0;

        try
        {
            channel.BasicAck(deliveryTag, false);
        }
        catch (Exception e)
        {
            Logger?.LogError(e, "Failed to ack message");
        }
    }

    private void AsyncFailure(IMessage request, RC.IModel channel, Exception exception)
    {
        Logger?.LogError(exception, "Async method was completed with an exception for {request} ", request);

        try
        {
            channel.BasicNack(request.Headers.DeliveryTag().Value, false, ContainerUtils.ShouldRequeue(DefaultRequeueRejected, exception, Logger));
        }
        catch (Exception e)
        {
            Logger?.LogError(e, "Failed to nack message");
        }
    }

    protected class ReplyExpressionRoot
    {
        public IMessage Request { get; }

        public object Source { get; }

        public object Result { get; }

        public ReplyExpressionRoot(IMessage request, object source, object result)
        {
            Request = request;
            Source = source;
            Result = result;
        }
    }
}

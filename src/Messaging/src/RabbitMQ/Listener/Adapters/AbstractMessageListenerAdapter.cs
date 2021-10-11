// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Configuration;
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
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RC=RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Listener.Adapters
{
    public abstract class AbstractMessageListenerAdapter : IChannelAwareMessageListener
    {
        protected readonly ILogger _logger;

        private const string DEFAULT_ENCODING = "UTF-8";

        private static readonly SpelExpressionParser PARSER = new ();
        private static readonly IParserContext PARSER_CONTEXT = new TemplateParserContext("!{", "}");

        protected AbstractMessageListenerAdapter(IApplicationContext context, ILogger logger = null)
        {
            _logger = logger;
            MessageConverter = new RabbitMQ.Support.Converter.SimpleMessageConverter();
            ApplicationContext = context;
        }

        public IApplicationContext ApplicationContext { get; set; }

        public virtual string Encoding { get; set; } = DEFAULT_ENCODING;

        public virtual string ResponseRoutingKey { get; set; } = string.Empty;

        public virtual string ResponseExchange { get; set; }

        public virtual Address ResponseAddress { get; set; }

        public virtual bool MandatoryPublish { get; set; }

        public virtual ISmartMessageConverter MessageConverter { get; set; }

        public virtual List<IMessagePostProcessor> BeforeSendReplyPostProcessors { get; private set; }

        public virtual RetryTemplate RetryTemplate { get; set; }

        public virtual IRecoveryCallback RecoveryCallback { get; set; }

        public virtual bool DefaultRequeueRejected { get; set; } = true;

        public virtual AcknowledgeMode ContainerAckMode { get; set; }

        public virtual bool IsManualAck => ContainerAckMode == AcknowledgeMode.MANUAL;

        public virtual StandardEvaluationContext EvalContext { get; set; } = new StandardEvaluationContext();

        public virtual IMessageHeadersConverter MessagePropertiesConverter { get; set; } = new DefaultMessageHeadersConverter();

        public virtual IExpression ResponseExpression { get; set; }

        public virtual IReplyPostProcessor ReplyPostProcessor { get; set; }

        public virtual void SetResponseAddress(string defaultReplyTo)
        {
            if (defaultReplyTo.StartsWith(PARSER_CONTEXT.ExpressionPrefix))
            {
                ResponseExpression = PARSER.ParseExpression(defaultReplyTo, PARSER_CONTEXT);
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
            if (beforeSendReplyPostProcessors == null)
            {
                throw new ArgumentNullException(nameof(beforeSendReplyPostProcessors));
            }

            foreach (var elem in beforeSendReplyPostProcessors)
            {
                if (elem == null)
                {
                    throw new ArgumentNullException("'replyPostProcessors' must not have any null elements");
                }
            }

            BeforeSendReplyPostProcessors = new List<IMessagePostProcessor>(beforeSendReplyPostProcessors);
        }

        public abstract void OnMessage(IMessage message, RC.IModel channel);

        public virtual void OnMessage(IMessage message)
        {
            throw new InvalidOperationException("Should never be called for a ChannelAwareMessageListener");
        }

        public virtual void OnMessageBatch(List<IMessage> messages, RC.IModel channel)
        {
            throw new NotSupportedException("This listener does not support message batches");
        }

        public virtual void OnMessageBatch(List<IMessage> messages)
        {
            throw new NotSupportedException("This listener does not support message batches");
        }

        protected internal virtual IMessage<byte[]> BuildMessage(RC.IModel channel, object result, Type genericType)
        {
            var converter = MessageConverter;
            if (converter != null && result is not IMessage<byte[]>)
            {
                result = converter.ToMessage(result, new MessageHeaders(), genericType);
            }

            if (result is not IMessage<byte[]>)
            {
                throw new MessageConversionException("No MessageConverter specified - cannot handle message [" + result + "]");
            }

            return (IMessage<byte[]>)result;
        }

        protected virtual void HandleListenerException(Exception exception)
        {
            _logger?.LogError(exception, "Listener execution failed");
        }

        protected virtual object ExtractMessage(IMessage message)
        {
            var converter = MessageConverter;
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
                if (resultArg.ReturnValue is Task)
                {
                    if (!IsManualAck)
                    {
                        _logger?.LogWarning("Container AcknowledgeMode must be MANUAL for a Future<?> return type; "
                                + "otherwise the container will ack the message immediately");
                    }

                    var asTask = resultArg.ReturnValue as Task;
                    asTask.ContinueWith(
                        (t) =>
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
                _logger?.LogWarning("Listener method returned result [" + resultArg
                        + "]: not generating response message for it because no Rabbit Channel given");
            }
        }

        protected virtual void DoHandleResult(InvocationResult resultArg, IMessage request, RC.IModel channel, object source)
        {
            _logger?.LogDebug("Listener method returned result [{result}] - generating response message for it", resultArg);
            try
            {
                var response = BuildMessage(channel, resultArg.ReturnValue, resultArg.ReturnType);
                var accessor = RabbitHeaderAccessor.GetMutableAccessor(response);
                accessor.Target = resultArg.Instance;
                accessor.TargetMethod = resultArg.Method;
                PostProcessResponse(request, response);
                var replyTo = GetReplyToAddress(request, source, resultArg);
                SendResponse(channel, replyTo, response);
            }
            catch (Exception ex)
            {
                throw new ReplyFailureException("Failed to send reply with payload '" + resultArg + "'", ex);
            }
        }

        protected virtual string GetReceivedExchange(IMessage request)
        {
            return request.Headers.ReceivedExchange();
        }

        protected virtual void PostProcessResponse(IMessage request, IMessage response)
        {
            var correlation = request.Headers.CorrelationId();

            if (correlation == null)
            {
                var messageId = request.Headers.MessageId();
                if (messageId != null)
                {
                    correlation = messageId;
                }
            }

            var accessor = RabbitHeaderAccessor.GetMutableAccessor(response);
            accessor.CorrelationId = correlation;
        }

        protected virtual Address GetReplyToAddress(IMessage request, object source, InvocationResult result)
        {
            var replyTo = request.Headers.ReplyToAddress();
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
                    throw new RabbitException(
                            "Cannot determine ReplyTo message property value: " +
                                    "Request message does not contain reply-to property, " +
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
            var message = messageIn;
            if (BeforeSendReplyPostProcessors != null)
            {
                var processors = BeforeSendReplyPostProcessors;
                IMessage postProcessed = message;
                foreach (var postProcessor in processors)
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
                _logger?.LogDebug("Publishing response to exchange = [{exchange}], routingKey = [{routingKey}]", replyTo.ExchangeName, replyTo.RoutingKey);
                if (RetryTemplate == null)
                {
                    DoPublish(channel, replyTo, message);
                }
                else
                {
                    var messageToSend = message;
                    RetryTemplate.Execute<object>(
                        ctx =>
                        {
                            DoPublish(channel, replyTo, messageToSend);
                            return null;
                        },
                        ctx =>
                        {
                            if (RecoveryCallback != null)
                            {
                                ctx.SetAttribute(SendRetryContextAccessor.MESSAGE, messageToSend);
                                ctx.SetAttribute(SendRetryContextAccessor.ADDRESS, replyTo);
                                RecoveryCallback.Recover(ctx);
                                return null;
                            }
                            else
                            {
                                throw RabbitExceptionTranslator.ConvertRabbitAccessException(ctx.LastException);
                            }
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
            var props = channel.CreateBasicProperties();
            MessagePropertiesConverter.FromMessageHeaders(message.Headers, props, EncodingUtils.GetEncoding(Encoding));
            channel.BasicPublish(replyTo.ExchangeName, replyTo.RoutingKey, MandatoryPublish, props, message.Payload);
        }

        protected virtual void PostProcessChannel(RC.IModel channel, IMessage response)
        {
        }

        private static object GetReturnValue(Task task)
        {
            var taskType = task.GetType();
            if (!taskType.IsGenericType)
            {
                return null;
            }

            var property = taskType.GetProperty("Result");
            return property.GetValue(task);
        }

        private Address EvaluateReplyTo(IMessage request, object source, object result, IExpression expression)
        {
            var value = expression.GetValue(EvalContext, new ReplyExpressionRoot(request, source, result));
            var replyTo = value switch
            {
                not string and not Address => throw new ArgumentException("response expression must evaluate to a String or Address"),
                string sValue => new Address(sValue),
                _ => (Address)value,
            };
            return replyTo;
        }

        private void AsyncSuccess(InvocationResult resultArg, IMessage request, RC.IModel channel, object source, object deferredResult)
        {
            if (deferredResult == null)
            {
                _logger?.LogDebug("Async result is null, ignoring");
            }
            else
            {
                var returnType = resultArg.ReturnType;
                if (returnType != null)
                {
                    var actualTypeArguments = returnType.ContainsGenericParameters ? returnType.GetGenericArguments() : Array.Empty<Type>();
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
            var tag = request.Headers.DeliveryTag();
            var deliveryTag = tag ?? 0;
            try
            {
                channel.BasicAck(deliveryTag, false);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Failed to ack message");
            }
        }

        private void AsyncFailure(IMessage request, RC.IModel channel, Exception exception)
        {
            _logger?.LogError(exception, "Async method was completed with an exception for {request} ", request);
            try
            {
                channel.BasicNack(request.Headers.DeliveryTag().Value, false, ContainerUtils.ShouldRequeue(DefaultRequeueRejected, exception, _logger));
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Failed to nack message");
            }
        }

        protected class ReplyExpressionRoot
        {
            public ReplyExpressionRoot(IMessage request, object source, object result)
            {
                Request = request;
                Source = source;
                Result = result;
            }

            public IMessage Request { get; }

            public object Source { get; }

            public object Result { get; }
        }
    }
}

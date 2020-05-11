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
using Steeltoe.Common.Expression;
using Steeltoe.Common.Retry;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Rabbit.Data;
using Steeltoe.Messaging.Rabbit.Exceptions;
using Steeltoe.Messaging.Rabbit.Expressions;
using Steeltoe.Messaging.Rabbit.Listener.Support;
using Steeltoe.Messaging.Rabbit.Support;
using Steeltoe.Messaging.Rabbit.Support.Converter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Rabbit.Listener.Adapters
{
    public abstract class AbstractMessageListenerAdapter : IChannelAwareMessageListener
    {
        protected readonly ILogger _logger;
        private const string DEFAULT_ENCODING = "UTF-8";

        protected AbstractMessageListenerAdapter(ILogger logger = null)
        {
            _logger = logger;
        }

        public virtual string Encoding { get; set; } = DEFAULT_ENCODING;

        public virtual string ResponseRoutingKey { get; set; } = string.Empty;

        public virtual string ResponseExchange { get; set; }

        public virtual Address ResponseAddress { get; set; }

        public virtual bool MandatoryPublish { get; set; }

        public virtual IMessageConverter MessageConverter { get; set; }

        public virtual List<IMessagePostProcessor> BeforeSendReplyPostProcessors { get; private set; }

        public virtual RetryTemplate RetryTemplate { get; set; }

        public virtual IRecoveryCallback RecoveryCallback { get; set; }

        public virtual bool DefaultRequeueRejected { get; set; }

        public virtual AcknowledgeMode ContainerAckMode { get; set; }

        public virtual bool IsManualAck => ContainerAckMode == AcknowledgeMode.MANUAL;

        public virtual IEvaluationContext EvalContext { get; set; }

        public virtual IMessagePropertiesConverter MessagePropertiesConverter { get; set; } = new DefaultMessagePropertiesConverter();

        public virtual IExpression ResponseExpression { get; set; }

        public virtual IReplyPostProcessor ReplyPostProcessor { get; set; }

        public virtual void SetResponseAddress(string defaultReplyTo)
        {
            // TODO: Java has Expression support?
            ResponseAddress = new Address(defaultReplyTo);
        }

        public void SetServiceResolver(IServiceResolver serviceResolver)
        {
            throw new NotImplementedException();

            // EvalContext.setBeanResolver(ServiceResolver);
            // EvalContext.setTypeConverter(new StandardTypeConverter());
            // EvalContext.addPropertyAccessor(new MapAccessor());
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

        public abstract void OnMessage(Message message, IModel channel);

        public virtual void OnMessage(Message message)
        {
            throw new InvalidOperationException("Should never be called for a ChannelAwareMessageListener");
        }

        public virtual void OnMessageBatch(List<Message> messages, IModel channel)
        {
            throw new NotSupportedException("This listener does not support message batches");
        }

        public virtual void OnMessageBatch(List<Message> messages)
        {
            throw new NotSupportedException("This listener does not support message batches");
        }

        protected virtual void HandleListenerException(Exception exception)
        {
            _logger?.LogError("Listener execution failed", exception);
        }

        protected virtual object ExtractMessage(Message message)
        {
            var converter = MessageConverter;
            if (converter != null)
            {
                return converter.FromMessage(message);
            }

            return message;
        }

        protected virtual void HandleResult(InvocationResult resultArg, Message request, IModel channel)
        {
            HandleResult(resultArg, request, channel, null);
        }

        protected virtual void HandleResult(InvocationResult resultArg, Message request, IModel channel, object source)
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
                                AsyncSuccess(resultArg, request, channel, source, t);
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

        protected virtual void DoHandleResult(InvocationResult resultArg, Message request, IModel channel, object source)
        {
            _logger?.LogDebug("Listener method returned result [" + resultArg + "] - generating response message for it");
            try
            {
                var response = BuildMessage(channel, resultArg.ReturnValue, resultArg.ReturnType);
                var props = response.MessageProperties;
                props.Target = resultArg.Instance;
                props.TargetMethod = resultArg.Method;
                PostProcessResponse(request, response);
                var replyTo = GetReplyToAddress(request, source, resultArg);
                SendResponse(channel, replyTo, response);
            }
            catch (Exception ex)
            {
                throw new ReplyFailureException("Failed to send reply with payload '" + resultArg + "'", ex);
            }
        }

        protected virtual string GetReceivedExchange(Message request)
        {
            return request.MessageProperties.ReceivedExchange;
        }

        protected virtual Message BuildMessage(IModel channel, object result, Type genericType)
        {
            var converter = MessageConverter;
            if (converter != null && !(result is Message))
            {
                return converter.ToMessage(result, new MessageProperties(), genericType);
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

        protected virtual void PostProcessResponse(Message request, Message response)
        {
            var correlation = request.MessageProperties.CorrelationId;

            if (correlation == null)
            {
                var messageId = request.MessageProperties.MessageId;
                if (messageId != null)
                {
                    correlation = messageId;
                }
            }

            response.MessageProperties.CorrelationId = correlation;
        }

        protected virtual Address GetReplyToAddress(Message request, object source, InvocationResult result)
        {
            var replyTo = request.MessageProperties.ReplyToAddress;
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
                else if (ResponseExpression != null)
                {
                    replyTo = EvaluateReplyTo(request, source, result.ReturnValue, ResponseExpression);
                }
                else if (ResponseAddress == null)
                {
                    throw new AmqpException(
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

        protected void SendResponse(IModel channel, Address replyTo, Message messageIn)
        {
            var message = messageIn;
            if (BeforeSendReplyPostProcessors != null)
            {
                var processors = BeforeSendReplyPostProcessors;
                foreach (var postProcessor in processors)
                {
                    message = postProcessor.PostProcessMessage(message);
                }
            }

            PostProcessChannel(channel, message);

            try
            {
                _logger?.LogDebug("Publishing response to exchange = [" + replyTo.ExchangeName + "], routingKey = [" + replyTo.RoutingKey + "]");
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

        protected virtual void DoPublish(IModel channel, Address replyTo, Message message)
        {
            channel.BasicPublish(
                replyTo.ExchangeName,
                replyTo.RoutingKey,
                MandatoryPublish,
                MessagePropertiesConverter.FromMessageProperties(message.MessageProperties, channel.CreateBasicProperties(), EncodingUtils.GetEncoding(Encoding)),
                message.Body);
        }

        protected virtual void PostProcessChannel(IModel channel, Message response)
        {
        }

        private Address EvaluateReplyTo(Message request, object source, object result, IExpression expression)
        {
            Address replyTo;
            var value = expression.GetValue(EvalContext, new ReplyExpressionRoot(request, source, result));
            if (!(value is string) || !(value is Address))
            {
                throw new ArgumentException("response expression must evaluate to a String or Address");
            }

            if (value is string)
            {
                replyTo = new Address((string)value);
            }
            else
            {
                replyTo = (Address)value;
            }

            return replyTo;
        }

        private void AsyncSuccess(InvocationResult resultArg, Message request, IModel channel, object source, object deferredResult)
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

        private void BasicAck(Message request, IModel channel)
        {
            try
            {
                channel.BasicAck(request.MessageProperties.DeliveryTag.Value, false);
            }
            catch (IOException e)
            {
                _logger?.LogError("Failed to ack message", e);
            }
        }

        private void AsyncFailure(Message request, IModel channel, Exception exception)
        {
            _logger?.LogError("Future or Mono was completed with an exception for " + request, exception);
            try
            {
                channel.BasicNack(request.MessageProperties.DeliveryTag.Value, false, ContainerUtils.ShouldRequeue(DefaultRequeueRejected, exception, _logger));
            }
            catch (IOException e)
            {
                _logger?.LogError("Failed to nack message", e);
            }
        }

        protected class ReplyExpressionRoot
        {
            public ReplyExpressionRoot(Message request, object source, object result)
            {
                Request = request;
                Source = source;
                Result = result;
            }

            public Message Request { get; }

            public object Source { get; }

            public object Result { get; }
        }
    }
}

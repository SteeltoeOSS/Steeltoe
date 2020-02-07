﻿// Copyright 2017 the original author or authors.
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

using Microsoft.Extensions.Options;
using Steeltoe.Common.Expression;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Support;
using Steeltoe.Stream.Binder;
using Steeltoe.Stream.Config;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Stream.Binding
{
    public class MessageConverterConfigurer : IMessageChannelAndSourceConfigurer
    {
        private readonly IOptionsMonitor<BindingServiceOptions> _optionsMonitor;
        private readonly IMessageConverterFactory _messageConverterFactory;
        private readonly IEnumerable<IPartitionKeyExtractorStrategy> _extractors;
        private readonly IEnumerable<IPartitionSelectorStrategy> _seledctors;
        private readonly IExpressionParser _expressionParser;
        private readonly IEvaluationContext _evaluationContext;

        private BindingServiceOptions Options
        {
            get
            {
                return _optionsMonitor.CurrentValue;
            }
        }

        public MessageConverterConfigurer(
            IExpressionParser expressionParser,
            IEvaluationContext evaluationContext,
            IOptionsMonitor<BindingServiceOptions> optionsMonitor,
            IMessageConverterFactory messageConverterFactory,
            IEnumerable<IPartitionKeyExtractorStrategy> extractors,
            IEnumerable<IPartitionSelectorStrategy> seledctors)
        {
            _expressionParser = expressionParser;
            _evaluationContext = evaluationContext;
            _optionsMonitor = optionsMonitor;
            _messageConverterFactory = messageConverterFactory;
            _extractors = extractors;
            _seledctors = seledctors;
        }

        public void ConfigureInputChannel(IMessageChannel messageChannel, string channelName)
        {
            ConfigureMessageChannel(messageChannel, channelName, true);
        }

        public void ConfigureOutputChannel(IMessageChannel messageChannel, string channelName)
        {
            ConfigureMessageChannel(messageChannel, channelName, false);
        }

        public void ConfigurePolledMessageSource(IPollableMessageSource binding, string name)
        {
            IBindingOptions bindingOptions = Options.GetBindingOptions(name);
            var contentType = bindingOptions.ContentType;
            var consumerOptions = bindingOptions.Consumer;
            if ((consumerOptions == null || !consumerOptions.UseNativeDecoding)
                    && binding is DefaultPollableMessageSource)
            {
                ((DefaultPollableMessageSource)binding).AddInterceptor(
                    new InboundContentTypeEnhancingInterceptor(contentType));
            }
        }

        private void ConfigureMessageChannel(IMessageChannel channel, string channelName, bool inbound)
        {
            if (!(channel is Integration.Channel.AbstractMessageChannel messageChannel))
            {
                throw new ArgumentException(nameof(channel) + " not an AbstractMessageChannel");
            }

            IBindingOptions bindingOptions = Options.GetBindingOptions(channelName);
            var contentType = bindingOptions.ContentType;
            var producerOptions = bindingOptions.Producer;
            if (!inbound && producerOptions != null && producerOptions.IsPartitioned)
            {
                messageChannel.AddInterceptor(
                    new PartitioningInterceptor(
                        _expressionParser,
                        _evaluationContext,
                        bindingOptions,
                        GetPartitionKeyExtractorStrategy(producerOptions),
                        GetPartitionSelectorStrategy(producerOptions)));
            }

            var consumerOptions = bindingOptions.Consumer;
            if (IsNativeEncodingNotSet(producerOptions, consumerOptions, inbound))
            {
                if (inbound)
                {
                    messageChannel.AddInterceptor(
                        new InboundContentTypeEnhancingInterceptor(contentType));
                }
                else
                {
                    messageChannel.AddInterceptor(
                        new OutboundContentTypeConvertingInterceptor(
                            contentType,
                            _messageConverterFactory.MessageConverterForAllRegistered));
                }
            }
        }

        private IPartitionKeyExtractorStrategy GetPartitionKeyExtractorStrategy(IProducerOptions options)
        {
            IPartitionKeyExtractorStrategy strategy = null;
            if (!string.IsNullOrEmpty(options.PartitionKeyExtractorName))
            {
                strategy = _extractors.FirstOrDefault((s) => s.Name == options.PartitionKeyExtractorName);
                if (strategy == null)
                {
                    throw new InvalidOperationException("PartitionKeyExtractorStrategy bean with the name '" + options.PartitionKeyExtractorName + "' can not be found.");
                }
            }
            else
            {
                if (_extractors.Count() > 1)
                {
                    throw new InvalidOperationException("Multiple `IPartitionKeyExtractorStrategy` found from service container.");
                }

                if (_extractors.Count() == 1)
                {
                    strategy = _extractors.Single();
                }
            }

            return strategy;
        }

        private IPartitionSelectorStrategy GetPartitionSelectorStrategy(IProducerOptions options)
        {
            IPartitionSelectorStrategy strategy = null;
            if (!string.IsNullOrEmpty(options.PartitionSelectorName))
            {
                strategy = _seledctors.FirstOrDefault((s) => s.Name == options.PartitionSelectorName);
                if (strategy == null)
                {
                    throw new InvalidOperationException("IPartitionSelectorStrategy bean with the name '" + options.PartitionSelectorName + "' can not be found.");
                }
            }
            else
            {
                if (_seledctors.Count() > 1)
                {
                    throw new InvalidOperationException("Multiple `IPartitionSelectorStrategy` found from service container.");
                }

                if (_seledctors.Count() == 1)
                {
                    strategy = _seledctors.Single();
                }
                else
                {
                    strategy = new DefaultPartitionSelector();
                }
            }

            return strategy;
        }

        private bool IsNativeEncodingNotSet(IProducerOptions producerOptions, IConsumerOptions consumerOptions, bool input)
        {
            if (input)
            {
                return consumerOptions == null
                        || !consumerOptions.UseNativeDecoding;
            }
            else
            {
                return producerOptions == null
                        || !producerOptions.UseNativeEncoding;
            }
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    internal class DefaultPartitionSelector : IPartitionSelectorStrategy
    {
        public string Name => "DefaultPartitionSelector";

        public int SelectPartition(object key, int partitionCount)
        {
            var hashcode = key.GetHashCode();
            if (hashcode == int.MinValue)
            {
                hashcode = 0;
            }

            return Math.Abs(hashcode);
        }
    }

    internal abstract class AbstractContentTypeInterceptor : AbstractChannelInterceptor
    {
        protected readonly MimeType _mimeType;

        public AbstractContentTypeInterceptor(string contentType)
        {
            _mimeType = MimeTypeUtils.ParseMimeType(contentType);
        }

        public override IMessage PreSend(IMessage message, IMessageChannel channel)
        {
            return message is ErrorMessage ? message : DoPreSend(message, channel);
        }

        public abstract IMessage DoPreSend(IMessage message, IMessageChannel channel);
    }

    internal class OutboundContentTypeConvertingInterceptor : AbstractContentTypeInterceptor
    {
        private readonly IMessageConverter _messageConverter;

        public OutboundContentTypeConvertingInterceptor(string contentType, IMessageConverter messageConverter)
        : base(contentType)
        {
            _messageConverter = messageConverter;
        }

        public override IMessage DoPreSend(IMessage message, IMessageChannel channel)
        {
            // If handler is a function, FunctionInvoker will already perform message
            // conversion.
            // In fact in the future we should consider propagating knowledge of the
            // default content type
            // to MessageConverters instead of interceptors
            if (message.Payload is byte[] && message.Headers.ContainsKey(MessageHeaders.CONTENT_TYPE))
            {
                return message;
            }

            // ===== 1.3 backward compatibility code part-1 ===
            string oct = null;
            if (message.Headers.ContainsKey(MessageHeaders.CONTENT_TYPE))
            {
                oct = message.Headers.Get<MimeType>(MessageHeaders.CONTENT_TYPE).ToString();
            }

            var ct = oct;
            if (message.Payload is string)
            {
                if (MimeTypeUtils.APPLICATION_JSON_VALUE.Equals(oct))
                {
                    ct = MimeTypeUtils.APPLICATION_JSON_VALUE;
                }
                else
                {
                    ct = MimeTypeUtils.TEXT_PLAIN_VALUE;
                }
            }

            // ===== END 1.3 backward compatibility code part-1 ===
            if (!message.Headers.ContainsKey(MessageHeaders.CONTENT_TYPE))
            {
                var messageHeaders = message.Headers as MessageHeaders;
                messageHeaders.RawHeaders[MessageHeaders.CONTENT_TYPE] = _mimeType;
            }

            IMessage result;
            if (message.Payload is byte[])
            {
                result = message;
            }
            else
            {
                result = _messageConverter.ToMessage(message.Payload, message.Headers);
            }

            if (result == null)
            {
                throw new InvalidOperationException("Failed to convert message: '" + message + "' to outbound message.");
            }

            // ===== 1.3 backward compatibility code part-2 ===
            if (ct != null && !ct.Equals(oct) && oct != null)
            {
                var messageHeaders = result.Headers as MessageHeaders;
                messageHeaders.RawHeaders[MessageHeaders.CONTENT_TYPE] = MimeType.ToMimeType(ct);
                messageHeaders.RawHeaders[BinderHeaders.BINDER_ORIGINAL_CONTENT_TYPE] = MimeType.ToMimeType(oct);
            }

            // ===== END 1.3 backward compatibility code part-2 ===
            return result;
        }
    }

    internal class InboundContentTypeEnhancingInterceptor : AbstractContentTypeInterceptor
    {
        public InboundContentTypeEnhancingInterceptor(string contentType)
        : base(contentType)
        {
        }

        public override IMessage DoPreSend(IMessage message, IMessageChannel channel)
        {
            var messageHeaders = message.Headers as MessageHeaders;
            var contentType = _mimeType;

            /*
             * NOTE: The below code for BINDER_ORIGINAL_CONTENT_TYPE is to support legacy
             * message format established in 1.x version of the Java Streams framework and should/will
             * no longer be supported in 3.x of Java Streams
             */

            if (message.Headers.ContainsKey(BinderHeaders.BINDER_ORIGINAL_CONTENT_TYPE))
            {
                var ct = message.Headers.Get<object>(BinderHeaders.BINDER_ORIGINAL_CONTENT_TYPE);
                if (ct is string)
                {
                    contentType = MimeType.ToMimeType((string)ct);
                }
                else if (ct is MimeType)
                {
                    contentType = (MimeType)ct;
                }

                messageHeaders.RawHeaders.Remove(BinderHeaders.BINDER_ORIGINAL_CONTENT_TYPE);
                if (messageHeaders.RawHeaders.ContainsKey(MessageHeaders.CONTENT_TYPE))
                {
                    messageHeaders.RawHeaders.Remove(MessageHeaders.CONTENT_TYPE);
                }
            }

            if (!message.Headers.ContainsKey(MessageHeaders.CONTENT_TYPE))
            {
                messageHeaders.RawHeaders.Add(MessageHeaders.CONTENT_TYPE, contentType);
            }
            else if (message.Headers.TryGetValue(MessageHeaders.CONTENT_TYPE, out var header) && header is string)
            {
                messageHeaders.RawHeaders[MessageHeaders.CONTENT_TYPE] = MimeType.ToMimeType((string)header);
            }

            return message;
        }
    }

    internal class PartitioningInterceptor : AbstractChannelInterceptor
    {
        internal readonly IBindingOptions _bindingOptions;

        internal readonly PartitionHandler _partitionHandler;
        internal readonly IMessageBuilderFactory _messageBuilderFactory = new MutableMessageBuilderFactory();

        public PartitioningInterceptor(IExpressionParser expressionParser, IEvaluationContext evaluationContext, IBindingOptions bindingOptions, IPartitionKeyExtractorStrategy partitionKeyExtractorStrategy, IPartitionSelectorStrategy partitionSelectorStrategy)
        {
            _bindingOptions = bindingOptions;
            _partitionHandler = new PartitionHandler(
                    expressionParser,
                    evaluationContext,
                    _bindingOptions.Producer,
                    partitionKeyExtractorStrategy,
                    partitionSelectorStrategy);
        }

        public int PartitionCount
        {
            get { return _partitionHandler.PartitionCount; }
            set { _partitionHandler.PartitionCount = value; }
        }

        public override IMessage PreSend(IMessage message, IMessageChannel channel)
        {
            var objMessage = (IMessage<object>)message;

            if (!message.Headers.ContainsKey(BinderHeaders.PARTITION_OVERRIDE))
            {
                var partition = _partitionHandler.DeterminePartition(message);
                return _messageBuilderFactory
                        .FromMessage(objMessage)
                        .SetHeader(BinderHeaders.PARTITION_HEADER, partition).Build();
            }
            else
            {
                return _messageBuilderFactory
                        .FromMessage(objMessage)
                        .SetHeader(BinderHeaders.PARTITION_HEADER, message.Headers[BinderHeaders.PARTITION_OVERRIDE])
                        .RemoveHeader(BinderHeaders.PARTITION_OVERRIDE).Build();
            }
        }
    }

#pragma warning restore SA1402 // File may only contain a single class
}

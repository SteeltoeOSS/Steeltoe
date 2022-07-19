// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Stream.Binder;
using Steeltoe.Stream.Config;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Stream.Binding;

public class MessageConverterConfigurer : IMessageChannelAndSourceConfigurer
{
    private readonly IOptionsMonitor<BindingServiceOptions> _optionsMonitor;
    private readonly IMessageConverterFactory _messageConverterFactory;
    private readonly IEnumerable<IPartitionKeyExtractorStrategy> _extractors;
    private readonly IEnumerable<IPartitionSelectorStrategy> _selectors;

    // private readonly IExpressionParser _expressionParser;
    // private readonly IEvaluationContext _evaluationContext;
    private readonly IApplicationContext _applicationContext;

    private BindingServiceOptions Options
    {
        get
        {
            return _optionsMonitor.CurrentValue;
        }
    }

    public MessageConverterConfigurer(
        IApplicationContext applicationContext,
        IOptionsMonitor<BindingServiceOptions> optionsMonitor,
        IMessageConverterFactory messageConverterFactory,
        IEnumerable<IPartitionKeyExtractorStrategy> extractors,
        IEnumerable<IPartitionSelectorStrategy> selectors)
    {
        _applicationContext = applicationContext;
        _optionsMonitor = optionsMonitor;
        _messageConverterFactory = messageConverterFactory;
        _extractors = extractors;
        _selectors = selectors;
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
            && binding is DefaultPollableMessageSource source)
        {
            source.AddInterceptor(
                new InboundContentTypeEnhancingInterceptor(contentType));
        }
    }

    private static bool IsNativeEncodingNotSet(IProducerOptions producerOptions, IConsumerOptions consumerOptions, bool input)
        => input
            ? consumerOptions == null || !consumerOptions.UseNativeDecoding
            : producerOptions == null || !producerOptions.UseNativeEncoding;

    private void ConfigureMessageChannel(IMessageChannel channel, string channelName, bool inbound)
    {
        if (channel is not Integration.Channel.AbstractMessageChannel messageChannel)
        {
            throw new ArgumentException($"{nameof(channel)} not an AbstractMessageChannel");
        }

        IBindingOptions bindingOptions = Options.GetBindingOptions(channelName);
        var contentType = bindingOptions.ContentType;
        var producerOptions = bindingOptions.Producer;
        if (!inbound && producerOptions != null && producerOptions.IsPartitioned)
        {
            messageChannel.AddInterceptor(
                new PartitioningInterceptor(
                    new SpelExpressionParser(),
                    null,
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
            strategy = _extractors?.FirstOrDefault(s => s.ServiceName == options.PartitionKeyExtractorName);
            if (strategy == null)
            {
                throw new InvalidOperationException($"PartitionKeyExtractorStrategy bean with the name '{options.PartitionKeyExtractorName}' can not be found.");
            }
        }
        else
        {
            if (_extractors?.Count() > 1)
            {
                throw new InvalidOperationException("Multiple `IPartitionKeyExtractorStrategy` found from service container.");
            }

            if (_extractors?.Count() == 1)
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
            strategy = _selectors.FirstOrDefault(s => s.ServiceName == options.PartitionSelectorName);
            if (strategy == null)
            {
                throw new InvalidOperationException($"IPartitionSelectorStrategy bean with the name '{options.PartitionSelectorName}' can not be found.");
            }
        }
        else
        {
            if (_selectors.Count() > 1)
            {
                throw new InvalidOperationException("Multiple `IPartitionSelectorStrategy` found from service container.");
            }

            strategy = _selectors.Count() == 1 ? _selectors.Single() : new DefaultPartitionSelector();
        }

        return strategy;
    }
}
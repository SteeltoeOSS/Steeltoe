// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Integration.Handler;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Messaging.Handler.Invocation;
using Steeltoe.Stream.Attributes;
using Steeltoe.Stream.Config;

namespace Steeltoe.Stream.Binding;

public class StreamListenerAttributeProcessor
{
    private readonly IApplicationContext _context;
    private readonly IOptionsMonitor<SpringIntegrationOptions> _springIntegrationOptionsMonitor;
    private readonly IDestinationResolver<IMessageChannel> _binderAwareChannelResolver;
    private readonly IMessageHandlerMethodFactory _messageHandlerMethodFactory;
    private readonly List<IStreamListenerSetupMethodOrchestrator> _methodOrchestrators;
    private readonly List<IStreamListenerMethod> _streamListenerMethods;
    internal readonly Dictionary<string, List<StreamListenerHandlerMethodMapping>> MappedListenerMethods = new();

    private SpringIntegrationOptions IntegrationOptions => _springIntegrationOptionsMonitor.CurrentValue;

#pragma warning disable S107 // Methods should not have too many parameters
    public StreamListenerAttributeProcessor(IApplicationContext context, IOptionsMonitor<SpringIntegrationOptions> springIntegrationOptionsMonitor,
        IEnumerable<IStreamListenerParameterAdapter> streamListenerParameterAdapters, IEnumerable<IStreamListenerResultAdapter> streamListenerResultAdapters,
        IDestinationResolver<IMessageChannel> binderAwareChannelResolver, IMessageHandlerMethodFactory messageHandlerMethodFactory,
        IEnumerable<IStreamListenerSetupMethodOrchestrator> methodOrchestrators, IEnumerable<IStreamListenerMethod> methods)
#pragma warning restore S107 // Methods should not have too many parameters
    {
        _context = context;
        _springIntegrationOptionsMonitor = springIntegrationOptionsMonitor;
        _binderAwareChannelResolver = binderAwareChannelResolver;
        _messageHandlerMethodFactory = messageHandlerMethodFactory;
        _streamListenerMethods = methods.ToList();
        _methodOrchestrators = methodOrchestrators.ToList();

        _methodOrchestrators.Add(
            new DefaultStreamListenerSetupMethodOrchestrator(this, context, streamListenerParameterAdapters, streamListenerResultAdapters));
    }

    public void Initialize()
    {
        if (_streamListenerMethods.Count <= 0)
        {
            return;
        }

        foreach (IStreamListenerMethod method in _streamListenerMethods)
        {
            DoPostProcess(method);
        }

        foreach (KeyValuePair<string, List<StreamListenerHandlerMethodMapping>> mappedBindingEntry in MappedListenerMethods)
        {
            var handlers = new List<DispatchingStreamListenerMessageHandler.ConditionalStreamListenerMessageHandlerWrapper>();

            foreach (StreamListenerHandlerMethodMapping mapping in mappedBindingEntry.Value)
            {
                object targetBean = CreateTargetBean(mapping.Implementation);

                IInvocableHandlerMethod invocableHandlerMethod = _messageHandlerMethodFactory.CreateInvocableHandlerMethod(targetBean, mapping.Method);

                var streamListenerMessageHandler = new StreamListenerMessageHandler(_context, invocableHandlerMethod, mapping.CopyHeaders,
                    IntegrationOptions.MessageHandlerNotPropagatedHeaders);

                if (!string.IsNullOrEmpty(mapping.DefaultOutputChannel))
                {
                    streamListenerMessageHandler.OutputChannelName = mapping.DefaultOutputChannel;
                }

                if (!string.IsNullOrEmpty(mapping.Condition))
                {
                    var parser = _context.GetService<IExpressionParser>();

                    if (parser == null)
                    {
                        throw new InvalidOperationException("StreamListener attribute contains a 'Condition', but no Expression parser is configured");
                    }

                    string conditionAsString = ResolveExpressionAsString(mapping.Condition);
                    IExpression condition = parser.ParseExpression(conditionAsString);

                    handlers.Add(new DispatchingStreamListenerMessageHandler.ConditionalStreamListenerMessageHandlerWrapper(condition,
                        streamListenerMessageHandler));
                }
                else
                {
                    handlers.Add(new DispatchingStreamListenerMessageHandler.ConditionalStreamListenerMessageHandlerWrapper(null,
                        streamListenerMessageHandler));
                }
            }

            if (handlers.Count > 1)
            {
                foreach (DispatchingStreamListenerMessageHandler.ConditionalStreamListenerMessageHandlerWrapper h in handlers)
                {
                    if (!h.IsVoid)
                    {
                        throw new ArgumentException(StreamListenerErrorMessages.MultipleValueReturningMethods);
                    }
                }
            }

            AbstractReplyProducingMessageHandler handler;

            if (handlers.Count > 1 || handlers[0].Condition != null)
            {
                var evaluationContext = _context.GetService<IEvaluationContext>();
                handler = new DispatchingStreamListenerMessageHandler(_context, handlers, evaluationContext);
            }
            else
            {
                handler = handlers[0].StreamListenerMessageHandler;
            }

            handler.IntegrationServices.ChannelResolver = _binderAwareChannelResolver;

            // handler.afterPropertiesSet();
            // this.applicationContext.getBeanFactory().registerSingleton(
            //        handler.getClass().getSimpleName() + handler.hashCode(), handler);
            // this.applicationContext
            //    .getBean(mappedBindingEntry.getKey(), typeof(ISubscribableChannel))
            if (BindingHelpers.GetBindable<IMessageChannel>(_context, mappedBindingEntry.Key) is not ISubscribableChannel channel)
            {
                throw new InvalidOperationException($"Unable to locate ISubscribableChannel with ServiceName: {mappedBindingEntry.Key}");
            }

            channel.Subscribe(handler);
        }

        MappedListenerMethods.Clear();
    }

    internal void AddMappedListenerMethod(string key, StreamListenerHandlerMethodMapping mappedMethod)
    {
        if (!MappedListenerMethods.TryGetValue(key, out List<StreamListenerHandlerMethodMapping> mappings))
        {
            mappings = new List<StreamListenerHandlerMethodMapping>();
            MappedListenerMethods.Add(key, mappings);
        }

        mappings.Add(mappedMethod);
    }

    protected virtual StreamListenerAttribute PostProcessAttribute(StreamListenerAttribute attribute)
    {
        return attribute;
    }

    private static string ResolveExpressionAsString(string value)
    {
        string resolvedValue = value;

        if (value.StartsWith("#{") && value.EndsWith("}"))
        {
            resolvedValue = value.Substring(2, value.Length - 2 - 1);
        }

        return resolvedValue;
    }

    private object CreateTargetBean(Type implementation)
    {
        try
        {
            return _context.GetService(implementation);
        }
        catch (Exception e)
        {
            // Log
            throw new InvalidOperationException($"Unable to CreateInstance of type containing StreamListener method, Type: {implementation}", e);
        }
    }

    private void DoPostProcess(IStreamListenerMethod listenerMethod)
    {
        MethodInfo method = listenerMethod.Method;
        StreamListenerAttribute streamListener = PostProcessAttribute(listenerMethod.Attribute);
        Type beanType = listenerMethod.ImplementationType;

        IEnumerable<IStreamListenerSetupMethodOrchestrator> orchestrators = _methodOrchestrators.Select(o => o.Supports(method) ? o : null);

        if (orchestrators.Count() != 1)
        {
            throw new InvalidOperationException("Unable to determine IStreamListenerSetupMethodOrchestrator to utilize");
        }

        IStreamListenerSetupMethodOrchestrator orchestrator = orchestrators.Single();
        orchestrator.OrchestrateStreamListener(streamListener, method, beanType);
    }
}

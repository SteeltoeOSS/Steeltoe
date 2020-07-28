// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression;
using Steeltoe.Integration.Handler;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Stream.Attributes;
using Steeltoe.Stream.Config;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Stream.Binding
{
    public class StreamListenerAttributeProcessor
    {
        internal readonly Dictionary<string, List<StreamListenerHandlerMethodMapping>> _mappedListenerMethods = new Dictionary<string, List<StreamListenerHandlerMethodMapping>>();

        private readonly IApplicationContext _context;
        private readonly IOptionsMonitor<SpringIntegrationOptions> _springIntegrationOptionsMonitor;
        private readonly IDestinationResolver<IMessageChannel> _binderAwareChannelResolver;
        private readonly IMessageHandlerMethodFactory _messageHandlerMethodFactory;
        private readonly List<IStreamListenerSetupMethodOrchestrator> _methodOrchestrators;
        private readonly List<IStreamListenerMethod> _streamListenerMethods;

        public StreamListenerAttributeProcessor(
            IApplicationContext context,
            IOptionsMonitor<SpringIntegrationOptions> springIntegrationOptionsMonitor,
            IEnumerable<IStreamListenerParameterAdapter> streamListenerParameterAdapters,
            IEnumerable<IStreamListenerResultAdapter> streamListenerResultAdapters,
            IDestinationResolver<IMessageChannel> binderAwareChannelResolver,
            IMessageHandlerMethodFactory messageHandlerMethodFactory,
            IEnumerable<IStreamListenerSetupMethodOrchestrator> methodOrchestrators,
            IEnumerable<IStreamListenerMethod> methods)
        {
            _context = context;
            _springIntegrationOptionsMonitor = springIntegrationOptionsMonitor;
            _binderAwareChannelResolver = binderAwareChannelResolver;
            _messageHandlerMethodFactory = messageHandlerMethodFactory;
            _streamListenerMethods = methods.ToList();
            _methodOrchestrators = methodOrchestrators.ToList();
            _methodOrchestrators.Add(new DefaultStreamListenerSetupMethodOrchestrator(this, context, streamListenerParameterAdapters, streamListenerResultAdapters));
        }

        private SpringIntegrationOptions IntegrationOptions
        {
            get
            {
                return _springIntegrationOptionsMonitor.CurrentValue;
            }
        }

        public void AfterSingletonsInstantiated()
        {
            if (_streamListenerMethods.Count <= 0)
            {
                return;
            }

            foreach (var method in _streamListenerMethods)
            {
                DoPostProcess(method);
            }

            foreach (var mappedBindingEntry in _mappedListenerMethods)
            {
                var handlers = new List<DispatchingStreamListenerMessageHandler.ConditionalStreamListenerMessageHandlerWrapper>();
                foreach (var mapping in mappedBindingEntry.Value)
                {
                    var targetBean = CreateTargetBean(mapping.Implementation);

                    var invocableHandlerMethod = _messageHandlerMethodFactory.CreateInvocableHandlerMethod(targetBean, mapping.Method);
                    var streamListenerMessageHandler = new StreamListenerMessageHandler(_context, invocableHandlerMethod, mapping.CopyHeaders, IntegrationOptions.MessageHandlerNotPropagatedHeaders);

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

                        var conditionAsString = ResolveExpressionAsString(mapping.Condition);
                        var condition = parser.ParseExpression(conditionAsString);
                        handlers.Add(new DispatchingStreamListenerMessageHandler.ConditionalStreamListenerMessageHandlerWrapper(condition, streamListenerMessageHandler));
                    }
                    else
                    {
                        handlers.Add(new DispatchingStreamListenerMessageHandler.ConditionalStreamListenerMessageHandlerWrapper(null, streamListenerMessageHandler));
                    }
                }

                if (handlers.Count > 1)
                {
                    foreach (var h in handlers)
                    {
                        if (!h.IsVoid)
                        {
                            throw new ArgumentException(StreamListenerErrorMessages.MULTIPLE_VALUE_RETURNING_METHODS);
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

                var channel = BindingHelpers.GetBindable<IMessageChannel>(_context, mappedBindingEntry.Key) as ISubscribableChannel;
                if (channel == null)
                {
                    throw new InvalidOperationException("Unable to locate ISubscribableChannel with ServiceName: " + mappedBindingEntry.Key);
                }

                channel.Subscribe(handler);
            }

            _mappedListenerMethods.Clear();
        }

        internal void AddMappedListenerMethod(string key, StreamListenerHandlerMethodMapping mappedMethod)
        {
            if (!_mappedListenerMethods.TryGetValue(key, out var mappings))
            {
                mappings = new List<StreamListenerHandlerMethodMapping>();
                _mappedListenerMethods.Add(key, mappings);
            }

            mappings.Add(mappedMethod);
        }

        protected virtual StreamListenerAttribute PostProcessAttribute(StreamListenerAttribute attribute)
        {
            return attribute;
        }

        private string ResolveExpressionAsString(string value)
        {
            var resolvedValue = value;

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
                throw new InvalidOperationException("Unable to CreateInstance of type containing StreamListener method, Type: " + implementation, e);
            }
        }

        private void DoPostProcess(IStreamListenerMethod listenerMethod)
        {
            var method = listenerMethod.Method;
            var streamListener = PostProcessAttribute(listenerMethod.Attribute);
            var beanType = listenerMethod.ImplementationType;

            var orchestrators = _methodOrchestrators.Select((o) => o.Supports(method) ? o : null);
            if (orchestrators.Count() == 0 || orchestrators.Count() > 1)
            {
                throw new InvalidOperationException("Unable to determine IStreamListenerSetupMethodOrchestrator to utilize");
            }

            var orchestrator = orchestrators.Single();
            orchestrator.OrchestrateStreamListener(streamListener, method, beanType);
        }
    }
}

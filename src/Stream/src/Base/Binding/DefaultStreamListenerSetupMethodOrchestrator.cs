// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Messaging;
using Steeltoe.Stream.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Steeltoe.Stream.Binding
{
    public partial class DefaultStreamListenerSetupMethodOrchestrator : AbstractStreamListenerSetupMethodOrchestrator
    {
        private readonly List<IStreamListenerParameterAdapter> _streamListenerParameterAdapters;
        private readonly List<IStreamListenerResultAdapter> _streamListenerResultAdapters;
        private readonly StreamListenerAttributeProcessor _processor;

        public DefaultStreamListenerSetupMethodOrchestrator(
            StreamListenerAttributeProcessor processor,
            IServiceProvider serviceProvider,
            IEnumerable<IStreamListenerParameterAdapter> streamListenerParameterAdapters,
            IEnumerable<IStreamListenerResultAdapter> streamListenerResultAdapters)
            : base(serviceProvider)
        {
            _streamListenerParameterAdapters = streamListenerParameterAdapters.ToList();
            _streamListenerResultAdapters = streamListenerResultAdapters.ToList();
            _processor = processor;
        }

        public override void OrchestrateStreamListener(StreamListenerAttribute streamListener, MethodInfo method, Type implementation)
        {
            var methodAnnotatedInboundName = streamListener.Target;

            var streamListenerMethod = new StreamListenerMethodValidator(method, _serviceProvider, _streamListenerParameterAdapters);
            streamListenerMethod.Validate(methodAnnotatedInboundName, streamListener.Condition);

            var isDeclarative = streamListenerMethod.CheckDeclarativeMethod(methodAnnotatedInboundName);
            if (isDeclarative)
            {
                var methodAnnotatedOutboundName = streamListenerMethod.GetOutboundBindingTargetName();
                var adaptedInboundArguments = AdaptAndRetrieveInboundArguments(method, methodAnnotatedInboundName, _streamListenerParameterAdapters.ToArray());
                InvokeStreamListenerResultAdapter(method, implementation, methodAnnotatedOutboundName, adaptedInboundArguments);
            }
            else
            {
                RegisterHandlerMethodOnListenedChannel(streamListenerMethod, streamListener, implementation);
            }
        }

        public override bool Supports(MethodInfo method)
        {
            return true;
        }

        private void InvokeStreamListenerResultAdapter(MethodInfo method, Type implementation, string outboundName, params object[] arguments)
        {
            try
            {
                var bean = ActivatorUtilities.CreateInstance(_serviceProvider, implementation);
                if (typeof(void).Equals(method.ReturnType))
                {
                    method.Invoke(bean, arguments);
                }
                else
                {
                    var result = method.Invoke(bean, arguments);
                    if (string.IsNullOrEmpty(outboundName))
                    {
                        var parameters = method.GetParameters();
                        for (var parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
                        {
                            var methodParameter = parameters[parameterIndex];
                            var attr = methodParameter.GetCustomAttribute<OutputAttribute>();
                            if (attr != null)
                            {
                                outboundName = attr.Name;
                            }
                        }
                    }

                    var targetBean = BindingHelpers.GetBindableTarget(_serviceProvider, outboundName);
                    foreach (var streamListenerResultAdapter in _streamListenerResultAdapters)
                    {
                        if (streamListenerResultAdapter.Supports(result.GetType(), targetBean.GetType()))
                        {
                            streamListenerResultAdapter.Adapt(result, targetBean);
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Log
                throw;
            }
        }

        private void RegisterHandlerMethodOnListenedChannel(StreamListenerMethodValidator streamListenerMethod, StreamListenerAttribute streamListener, Type implementation)
        {
            if (string.IsNullOrEmpty(streamListener.Target))
            {
                throw new ArgumentException("The binding target name cannot be null");
            }

            var method = streamListenerMethod.Method;

            var defaultOutputChannel = streamListenerMethod.GetOutboundBindingTargetName();
            if (typeof(void).Equals(method.ReturnType))
            {
                if (!string.IsNullOrEmpty(defaultOutputChannel))
                {
                    throw new ArgumentException("An output channel cannot be specified for a method that does not return a value");
                }
            }
            else
            {
                if (string.IsNullOrEmpty(defaultOutputChannel))
                {
                    throw new ArgumentException("An output channel must be specified for a method that can return a value");
                }
            }

            streamListenerMethod.ValidateStreamListenerMessageHandler();

            _processor.AddMappedListenerMethod(streamListener.Target, new StreamListenerHandlerMethodMapping(implementation, method, streamListener.Condition, defaultOutputChannel, streamListener.CopyHeaders));
        }
    }
}

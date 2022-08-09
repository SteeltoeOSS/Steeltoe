// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Stream.Attributes;

namespace Steeltoe.Stream.Binding;

public class DefaultStreamListenerSetupMethodOrchestrator : AbstractStreamListenerSetupMethodOrchestrator
{
    private readonly List<IStreamListenerParameterAdapter> _streamListenerParameterAdapters;
    private readonly List<IStreamListenerResultAdapter> _streamListenerResultAdapters;
    private readonly StreamListenerAttributeProcessor _processor;

    public DefaultStreamListenerSetupMethodOrchestrator(StreamListenerAttributeProcessor processor, IApplicationContext context,
        IEnumerable<IStreamListenerParameterAdapter> streamListenerParameterAdapters, IEnumerable<IStreamListenerResultAdapter> streamListenerResultAdapters)
        : base(context)
    {
        _streamListenerParameterAdapters = streamListenerParameterAdapters.ToList();
        _streamListenerResultAdapters = streamListenerResultAdapters.ToList();
        _processor = processor;
    }

    public override void OrchestrateStreamListener(StreamListenerAttribute streamListener, MethodInfo method, Type implementationType)
    {
        string methodAnnotatedInboundName = streamListener.Target;

        var streamListenerMethod = new StreamListenerMethodValidator(method, Context, _streamListenerParameterAdapters);
        streamListenerMethod.Validate(methodAnnotatedInboundName, streamListener.Condition);

        bool isDeclarative = streamListenerMethod.CheckDeclarativeMethod(methodAnnotatedInboundName);

        if (isDeclarative)
        {
            string methodAnnotatedOutboundName = streamListenerMethod.GetOutboundBindingTargetName();
            object[] adaptedInboundArguments = AdaptAndRetrieveInboundArguments(method, methodAnnotatedInboundName, _streamListenerParameterAdapters.ToArray());
            InvokeStreamListenerResultAdapter(method, implementationType, methodAnnotatedOutboundName, adaptedInboundArguments);
        }
        else
        {
            RegisterHandlerMethodOnListenedChannel(streamListenerMethod, streamListener, implementationType);
        }
    }

    public override bool Supports(MethodInfo method)
    {
        return true;
    }

    private void InvokeStreamListenerResultAdapter(MethodInfo method, Type implementation, string outboundName, params object[] arguments)
    {
        object bean = ActivatorUtilities.CreateInstance(Context.ServiceProvider, implementation);

        if (typeof(void).Equals(method.ReturnType))
        {
            method.Invoke(bean, arguments);
        }
        else
        {
            object result = method.Invoke(bean, arguments);

            if (string.IsNullOrEmpty(outboundName))
            {
                ParameterInfo[] parameters = method.GetParameters();

                foreach (ParameterInfo methodParameter in parameters)
                {
                    var attr = methodParameter.GetCustomAttribute<OutputAttribute>();

                    if (attr != null)
                    {
                        outboundName = attr.Name;
                    }
                }
            }

            object targetBean = BindingHelpers.GetBindableTarget(Context, outboundName);

            foreach (IStreamListenerResultAdapter streamListenerResultAdapter in _streamListenerResultAdapters)
            {
                if (streamListenerResultAdapter.Supports(result.GetType(), targetBean.GetType()))
                {
                    streamListenerResultAdapter.Adapt(result, targetBean);
                    break;
                }
            }
        }
    }

    private void RegisterHandlerMethodOnListenedChannel(StreamListenerMethodValidator streamListenerMethod, StreamListenerAttribute streamListener,
        Type implementation)
    {
        if (string.IsNullOrEmpty(streamListener.Target))
        {
            throw new ArgumentException($"{nameof(streamListener.Target)} in {nameof(streamListener)} must not be null or empty.", nameof(streamListener));
        }

        MethodInfo method = streamListenerMethod.Method;

        string defaultOutputChannel = streamListenerMethod.GetOutboundBindingTargetName();

        if (typeof(void).Equals(method.ReturnType))
        {
            if (!string.IsNullOrEmpty(defaultOutputChannel))
            {
                throw new InvalidOperationException("An output channel cannot be specified for a method that does not return a value.");
            }
        }
        else
        {
            if (string.IsNullOrEmpty(defaultOutputChannel))
            {
                throw new InvalidOperationException("An output channel must be specified for a method that can return a value.");
            }
        }

        streamListenerMethod.ValidateStreamListenerMessageHandler();

        _processor.AddMappedListenerMethod(streamListener.Target,
            new StreamListenerHandlerMethodMapping(implementation, method, streamListener.Condition, defaultOutputChannel, streamListener.CopyHeaders));
    }
}

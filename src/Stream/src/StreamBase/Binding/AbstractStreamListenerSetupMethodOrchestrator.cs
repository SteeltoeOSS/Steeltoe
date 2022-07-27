// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Stream.Attributes;
using System;
using System.Reflection;

namespace Steeltoe.Stream.Binding;

public abstract class AbstractStreamListenerSetupMethodOrchestrator : IStreamListenerSetupMethodOrchestrator
{
    protected readonly IApplicationContext _context;

    protected AbstractStreamListenerSetupMethodOrchestrator(IApplicationContext context)
    {
        _context = context;
    }

    public object[] AdaptAndRetrieveInboundArguments(MethodInfo method, string inboundName, params IStreamListenerParameterAdapter[] streamListenerParameterAdapters)
    {
        var arguments = new object[method.GetParameters().Length];
        for (var parameterIndex = 0; parameterIndex < arguments.Length; parameterIndex++)
        {
            var methodParameter = method.GetParameters()[parameterIndex];
            var parameterType = methodParameter.ParameterType;
            string targetReferenceValue = null;
            if (methodParameter.GetCustomAttribute<InputAttribute>() != null)
            {
                var attr = methodParameter.GetCustomAttribute<InputAttribute>();
                targetReferenceValue = attr.Name;
            }
            else if (methodParameter.GetCustomAttribute<OutputAttribute>() != null)
            {
                var attr = methodParameter.GetCustomAttribute<OutputAttribute>();
                targetReferenceValue = attr.Name;
            }
            else if (arguments.Length == 1 && !string.IsNullOrEmpty(inboundName))
            {
                targetReferenceValue = inboundName;
            }

            if (targetReferenceValue != null)
            {
                var targetBean = BindingHelpers.GetBindableTarget(_context, targetReferenceValue);

                // Iterate existing parameter adapters first
                foreach (var streamListenerParameterAdapter in streamListenerParameterAdapters)
                {
                    if (streamListenerParameterAdapter.Supports(targetBean.GetType(), methodParameter))
                    {
                        arguments[parameterIndex] = streamListenerParameterAdapter.Adapt(targetBean, methodParameter);
                        break;
                    }
                }

                if (arguments[parameterIndex] == null && parameterType.IsInstanceOfType(targetBean))
                {
                    arguments[parameterIndex] = targetBean;
                }

                if (arguments[parameterIndex] == null)
                {
                    throw new ArgumentException("Cannot convert argument " + parameterIndex + " of " + method
                                                + "from " + targetBean.GetType() + " to "
                                                + parameterType);
                }
            }
            else
            {
                throw new InvalidOperationException(StreamListenerErrorMessages.INVALID_DECLARATIVE_METHOD_PARAMETERS);
            }
        }

        return arguments;
    }

    public abstract void OrchestrateStreamListener(StreamListenerAttribute streamListener, MethodInfo method, Type bean);

    public abstract bool Supports(MethodInfo method);
}
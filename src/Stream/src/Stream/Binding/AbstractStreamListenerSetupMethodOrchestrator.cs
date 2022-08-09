// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common.Contexts;
using Steeltoe.Stream.Attributes;

namespace Steeltoe.Stream.Binding;

public abstract class AbstractStreamListenerSetupMethodOrchestrator : IStreamListenerSetupMethodOrchestrator
{
    protected readonly IApplicationContext Context;

    protected AbstractStreamListenerSetupMethodOrchestrator(IApplicationContext context)
    {
        Context = context;
    }

    public object[] AdaptAndRetrieveInboundArguments(MethodInfo method, string inboundName,
        params IStreamListenerParameterAdapter[] streamListenerParameterAdapters)
    {
        object[] arguments = new object[method.GetParameters().Length];

        for (int parameterIndex = 0; parameterIndex < arguments.Length; parameterIndex++)
        {
            ParameterInfo methodParameter = method.GetParameters()[parameterIndex];
            Type parameterType = methodParameter.ParameterType;
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
                object targetBean = BindingHelpers.GetBindableTarget(Context, targetReferenceValue);

                // Iterate existing parameter adapters first
                foreach (IStreamListenerParameterAdapter streamListenerParameterAdapter in streamListenerParameterAdapters)
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
                    throw new InvalidOperationException($"Cannot convert argument {parameterIndex} of {method} from {targetBean.GetType()} to {parameterType}");
                }
            }
            else
            {
                throw new InvalidOperationException(StreamListenerErrorMessages.InvalidDeclarativeMethodParameters);
            }
        }

        return arguments;
    }

    public abstract void OrchestrateStreamListener(StreamListenerAttribute streamListener, MethodInfo method, Type implementationType);

    public abstract bool Supports(MethodInfo method);
}

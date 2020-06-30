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

using Steeltoe.Common.Contexts;
using Steeltoe.Stream.Attributes;
using System;
using System.Reflection;

namespace Steeltoe.Stream.Binding
{
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

                    if (arguments[parameterIndex] == null && parameterType.IsAssignableFrom(targetBean.GetType()))
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
}

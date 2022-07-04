// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Stream.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Steeltoe.Stream.Binding;

public class StreamListenerMethodValidator
{
    private readonly IApplicationContext _context;
    private readonly List<IStreamListenerParameterAdapter> _streamListenerParameterAdapters;

    public StreamListenerMethodValidator(MethodInfo method, IApplicationContext context = null, List<IStreamListenerParameterAdapter> streamListenerParameterAdapters = null)
    {
        Method = method;
        _context = context;
        _streamListenerParameterAdapters = streamListenerParameterAdapters ?? new List<IStreamListenerParameterAdapter>();
    }

    public MethodInfo Method { get; }

    public int GetInputAnnotationCount()
    {
        var count = 0;
        var parameters = Method.GetParameters();
        foreach (var methodParameter in parameters)
        {
            if (methodParameter.GetCustomAttribute<InputAttribute>() != null)
            {
                count++;
            }
        }

        return count;
    }

    public int GetOutputAnnotationCount()
    {
        var count = 0;
        var parameters = Method.GetParameters();
        foreach (var methodParameter in parameters)
        {
            if (methodParameter.GetCustomAttribute<OutputAttribute>() != null)
            {
                count++;
            }
        }

        return count;
    }

    public void Validate(string methodAnnotatedInboundName, string condition)
    {
        var methodAnnotatedOutboundName = GetOutboundBindingTargetName();
        var inputAnnotationCount = GetInputAnnotationCount();
        var outputAnnotationCount = GetOutputAnnotationCount();
        var isDeclarative = CheckDeclarativeMethod(methodAnnotatedInboundName);

        var parameters = Method.GetParameters();
        var methodArgumentsLength = parameters.Length;
        var returnType = Method.ReturnType;
        if (!isDeclarative && !(inputAnnotationCount == 0 && outputAnnotationCount == 0))
        {
            throw new ArgumentException(StreamListenerErrorMessages.InvalidDeclarativeMethodParameters);
        }

        if (!string.IsNullOrEmpty(methodAnnotatedInboundName)
            && !string.IsNullOrEmpty(methodAnnotatedOutboundName) && !(inputAnnotationCount == 0 && outputAnnotationCount == 0))
        {
            throw new ArgumentException(StreamListenerErrorMessages.InvalidInputOutputMethodParameters);
        }

        if (!string.IsNullOrEmpty(methodAnnotatedInboundName))
        {
            if (inputAnnotationCount != 0)
            {
                throw new ArgumentException(StreamListenerErrorMessages.InvalidInputValues);
            }

            if (outputAnnotationCount != 0)
            {
                throw new ArgumentException(StreamListenerErrorMessages.InvalidInputValueWithOutputMethodParam);
            }
        }
        else
        {
            if (inputAnnotationCount < 1)
            {
                throw new ArgumentException(StreamListenerErrorMessages.NoInputDestination);
            }
        }

        if (!string.IsNullOrEmpty(methodAnnotatedOutboundName) && outputAnnotationCount != 0)
        {
            throw new ArgumentException(StreamListenerErrorMessages.InvalidOutputValues);
        }

        if (!typeof(void).Equals(returnType) && !string.IsNullOrEmpty(condition))
        {
            throw new ArgumentException(StreamListenerErrorMessages.ConditionOnMethodReturningValue);
        }

        if (isDeclarative)
        {
            if (!string.IsNullOrEmpty(condition))
            {
                throw new ArgumentException(StreamListenerErrorMessages.ConditionOnDeclarativeMethod);
            }

            for (var parameterIndex = 0; parameterIndex < methodArgumentsLength; parameterIndex++)
            {
                var methodParameter = parameters[parameterIndex];
                if (methodParameter.GetCustomAttribute<InputAttribute>() != null)
                {
                    var attribute = methodParameter.GetCustomAttribute<InputAttribute>();
                    var inboundName = attribute.Name;
                    if (string.IsNullOrEmpty(inboundName))
                    {
                        throw new ArgumentException(StreamListenerErrorMessages.InvalidInboundName);
                    }
                }

                if (methodParameter.GetCustomAttribute<OutputAttribute>() != null)
                {
                    var attribute = methodParameter.GetCustomAttribute<OutputAttribute>();
                    var outboundName = attribute.Name;
                    if (string.IsNullOrEmpty(outboundName))
                    {
                        throw new ArgumentException(StreamListenerErrorMessages.InvalidOutboundName);
                    }
                }
            }

            if (methodArgumentsLength > 1 && inputAnnotationCount + outputAnnotationCount != methodArgumentsLength)
            {
                throw new ArgumentException(StreamListenerErrorMessages.InvalidDeclarativeMethodParameters);
            }
        }

        if (!returnType.Equals(typeof(void)) && string.IsNullOrEmpty(methodAnnotatedOutboundName))
        {
            if (outputAnnotationCount == 0)
            {
                throw new ArgumentException(StreamListenerErrorMessages.ReturnTypeNoOutboundSpecified);
            }

            if (outputAnnotationCount != 1)
            {
                throw new ArgumentException(StreamListenerErrorMessages.ReturnTypeMultipleOutboundSpecified);
            }
        }
    }

    public void ValidateStreamListenerMessageHandler()
    {
        var parameters = Method.GetParameters();
        var methodArgumentsLength = parameters.Length;
        if (methodArgumentsLength > 1)
        {
            var numAnnotatedMethodParameters = 0;
            var numPayloadAnnotations = 0;
            for (var parameterIndex = 0; parameterIndex < methodArgumentsLength; parameterIndex++)
            {
                var methodParameter = parameters[parameterIndex];
                var attributes = methodParameter.GetCustomAttributes();
                if (attributes.Any())
                {
                    numAnnotatedMethodParameters++;
                }

                if (methodParameter.GetCustomAttribute<PayloadAttribute>() != null)
                {
                    numPayloadAnnotations++;
                }
            }

            if (numPayloadAnnotations > 0 && !(methodArgumentsLength == numAnnotatedMethodParameters && numPayloadAnnotations <= 1))
            {
                throw new ArgumentException(StreamListenerErrorMessages.AmbiguousMessageHandlerMethodArguments);
            }
        }
    }

    public string GetOutboundBindingTargetName()
    {
        var sendTo = Method.GetCustomAttribute<SendToAttribute>();
        if (sendTo != null)
        {
            if (ObjectUtils.IsNullOrEmpty(sendTo.Destinations))
            {
                throw new ArgumentException(StreamListenerErrorMessages.AtleastOneOutput);
            }

            if (sendTo.Destinations.Length != 1)
            {
                throw new ArgumentException(StreamListenerErrorMessages.SendToMultipleDestinations);
            }

            if (string.IsNullOrEmpty(sendTo.Destinations[0]))
            {
                throw new ArgumentException(StreamListenerErrorMessages.SendToEmptyDestination);
            }

            return sendTo.Destinations[0];
        }

        var output = Method.GetCustomAttribute<OutputAttribute>();
        if (output != null)
        {
            if (string.IsNullOrEmpty(output.Name))
            {
                throw new ArgumentException(StreamListenerErrorMessages.AtleastOneOutput);
            }

            return output.Name;
        }

        return null;
    }

    public bool CheckDeclarativeMethod(string methodAnnotatedInboundName)
    {
        var parameters = Method.GetParameters();
        var methodArgumentsLength = parameters.Length;
        var methodAnnotatedOutboundName = GetOutboundBindingTargetName();
        for (var parameterIndex = 0; parameterIndex < methodArgumentsLength; parameterIndex++)
        {
            var methodParameter = parameters[parameterIndex];
            if (methodParameter.GetCustomAttribute<InputAttribute>() != null)
            {
                var attribute = methodParameter.GetCustomAttribute<InputAttribute>();
                var inboundName = attribute.Name;
                if (string.IsNullOrEmpty(inboundName))
                {
                    throw new ArgumentException(StreamListenerErrorMessages.InvalidInboundName);
                }

                if (IsDeclarativeMethodParameter(inboundName, methodParameter))
                {
                    throw new ArgumentException(StreamListenerErrorMessages.InvalidDeclarativeMethodParameters);
                }

                return true;
            }
            else if (methodParameter.GetCustomAttribute<OutputAttribute>() != null)
            {
                var attribute = methodParameter.GetCustomAttribute<OutputAttribute>();
                var outboundName = attribute.Name;
                if (string.IsNullOrEmpty(outboundName))
                {
                    throw new ArgumentException(StreamListenerErrorMessages.InvalidOutboundName);
                }

                if (IsDeclarativeMethodParameter(outboundName, methodParameter))
                {
                    throw new ArgumentException(StreamListenerErrorMessages.InvalidDeclarativeMethodParameters);
                }

                return true;
            }
            else if (!string.IsNullOrEmpty(methodAnnotatedOutboundName))
            {
                return IsDeclarativeMethodParameter(methodAnnotatedOutboundName, methodParameter);
            }
            else if (!string.IsNullOrEmpty(methodAnnotatedInboundName))
            {
                return IsDeclarativeMethodParameter(methodAnnotatedInboundName, methodParameter);
            }
        }

        return false;
    }

    private bool IsDeclarativeMethodParameter(string target, ParameterInfo methodParameter)
    {
        var declarative = false;
        if (_context != null)
        {
            var targetBindable = BindingHelpers.GetBindableTarget(_context, target);
            if (!methodParameter.ParameterType.IsAssignableFrom(typeof(object)) && targetBindable != null)
            {
                declarative = typeof(IMessageChannel).IsAssignableFrom(methodParameter.ParameterType);
                if (!declarative)
                {
                    var targetBeanClass = targetBindable.GetType();
                    foreach (var adapter in _streamListenerParameterAdapters)
                    {
                        if (adapter.Supports(targetBeanClass, methodParameter))
                        {
                            declarative = true;
                            break;
                        }
                    }
                }
            }
        }
        else
        {
            if (!methodParameter.ParameterType.IsAssignableFrom(typeof(object)))
            {
                return typeof(IMessageChannel).IsAssignableFrom(methodParameter.ParameterType);
            }
        }

        return declarative;
    }
}

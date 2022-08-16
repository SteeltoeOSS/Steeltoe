// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Stream.Attributes;

namespace Steeltoe.Stream.Binding;

public class StreamListenerMethodValidator
{
    private readonly IApplicationContext _context;
    private readonly IEnumerable<IStreamListenerParameterAdapter> _streamListenerParameterAdapters;

    public MethodInfo Method { get; }

    public StreamListenerMethodValidator(MethodInfo method, IApplicationContext context = null,
        IEnumerable<IStreamListenerParameterAdapter> streamListenerParameterAdapters = null)
    {
        Method = method;
        _context = context;
        _streamListenerParameterAdapters = streamListenerParameterAdapters ?? new List<IStreamListenerParameterAdapter>();
    }

    public int GetInputAnnotationCount()
    {
        int count = 0;
        ParameterInfo[] parameters = Method.GetParameters();

        foreach (ParameterInfo methodParameter in parameters)
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
        int count = 0;
        ParameterInfo[] parameters = Method.GetParameters();

        foreach (ParameterInfo methodParameter in parameters)
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
        string methodAnnotatedOutboundName = GetOutboundBindingTargetName();
        int inputAnnotationCount = GetInputAnnotationCount();
        int outputAnnotationCount = GetOutputAnnotationCount();
        bool isDeclarative = CheckDeclarativeMethod(methodAnnotatedInboundName);

        ParameterInfo[] parameters = Method.GetParameters();
        int methodArgumentsLength = parameters.Length;
        Type returnType = Method.ReturnType;

        if (!isDeclarative && !(inputAnnotationCount == 0 && outputAnnotationCount == 0))
        {
            throw new InvalidOperationException(StreamListenerErrorMessages.InvalidDeclarativeMethodParameters);
        }

        if (!string.IsNullOrEmpty(methodAnnotatedInboundName) && !string.IsNullOrEmpty(methodAnnotatedOutboundName) &&
            !(inputAnnotationCount == 0 && outputAnnotationCount == 0))
        {
            throw new InvalidOperationException(StreamListenerErrorMessages.InvalidInputOutputMethodParameters);
        }

        if (!string.IsNullOrEmpty(methodAnnotatedInboundName))
        {
            if (inputAnnotationCount != 0)
            {
                throw new InvalidOperationException(StreamListenerErrorMessages.InvalidInputValues);
            }

            if (outputAnnotationCount != 0)
            {
                throw new InvalidOperationException(StreamListenerErrorMessages.InvalidInputValueWithOutputMethodParam);
            }
        }
        else
        {
            if (inputAnnotationCount < 1)
            {
                throw new InvalidOperationException(StreamListenerErrorMessages.NoInputDestination);
            }
        }

        if (!string.IsNullOrEmpty(methodAnnotatedOutboundName) && outputAnnotationCount != 0)
        {
            throw new InvalidOperationException(StreamListenerErrorMessages.InvalidOutputValues);
        }

        if (!typeof(void).Equals(returnType) && !string.IsNullOrEmpty(condition))
        {
            throw new InvalidOperationException(StreamListenerErrorMessages.ConditionOnMethodReturningValue);
        }

        if (isDeclarative)
        {
            if (!string.IsNullOrEmpty(condition))
            {
                throw new InvalidOperationException(StreamListenerErrorMessages.ConditionOnDeclarativeMethod);
            }

            for (int parameterIndex = 0; parameterIndex < methodArgumentsLength; parameterIndex++)
            {
                ParameterInfo methodParameter = parameters[parameterIndex];

                if (methodParameter.GetCustomAttribute<InputAttribute>() != null)
                {
                    var attribute = methodParameter.GetCustomAttribute<InputAttribute>();
                    string inboundName = attribute.Name;

                    if (string.IsNullOrEmpty(inboundName))
                    {
                        throw new InvalidOperationException(StreamListenerErrorMessages.InvalidInboundName);
                    }
                }

                if (methodParameter.GetCustomAttribute<OutputAttribute>() != null)
                {
                    var attribute = methodParameter.GetCustomAttribute<OutputAttribute>();
                    string outboundName = attribute.Name;

                    if (string.IsNullOrEmpty(outboundName))
                    {
                        throw new InvalidOperationException(StreamListenerErrorMessages.InvalidOutboundName);
                    }
                }
            }

            if (methodArgumentsLength > 1 && inputAnnotationCount + outputAnnotationCount != methodArgumentsLength)
            {
                throw new InvalidOperationException(StreamListenerErrorMessages.InvalidDeclarativeMethodParameters);
            }
        }

        if (!returnType.Equals(typeof(void)) && string.IsNullOrEmpty(methodAnnotatedOutboundName))
        {
            if (outputAnnotationCount == 0)
            {
                throw new InvalidOperationException(StreamListenerErrorMessages.ReturnTypeNoOutboundSpecified);
            }

            if (outputAnnotationCount != 1)
            {
                throw new InvalidOperationException(StreamListenerErrorMessages.ReturnTypeMultipleOutboundSpecified);
            }
        }
    }

    public void ValidateStreamListenerMessageHandler()
    {
        ParameterInfo[] parameters = Method.GetParameters();
        int methodArgumentsLength = parameters.Length;

        if (methodArgumentsLength > 1)
        {
            int numAnnotatedMethodParameters = 0;
            int numPayloadAnnotations = 0;

            for (int parameterIndex = 0; parameterIndex < methodArgumentsLength; parameterIndex++)
            {
                ParameterInfo methodParameter = parameters[parameterIndex];
                IEnumerable<Attribute> attributes = methodParameter.GetCustomAttributes();

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
                throw new InvalidOperationException(StreamListenerErrorMessages.AmbiguousMessageHandlerMethodArguments);
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
                throw new InvalidOperationException(StreamListenerErrorMessages.AtLeastOneOutput);
            }

            if (sendTo.Destinations.Length != 1)
            {
                throw new InvalidOperationException(StreamListenerErrorMessages.SendToMultipleDestinations);
            }

            if (string.IsNullOrEmpty(sendTo.Destinations[0]))
            {
                throw new InvalidOperationException(StreamListenerErrorMessages.SendToEmptyDestination);
            }

            return sendTo.Destinations[0];
        }

        var output = Method.GetCustomAttribute<OutputAttribute>();

        if (output != null)
        {
            if (string.IsNullOrEmpty(output.Name))
            {
                throw new InvalidOperationException(StreamListenerErrorMessages.AtLeastOneOutput);
            }

            return output.Name;
        }

        return null;
    }

    public bool CheckDeclarativeMethod(string methodAnnotatedInboundName)
    {
        ParameterInfo[] parameters = Method.GetParameters();
        int methodArgumentsLength = parameters.Length;
        string methodAnnotatedOutboundName = GetOutboundBindingTargetName();

        for (int parameterIndex = 0; parameterIndex < methodArgumentsLength; parameterIndex++)
        {
            ParameterInfo methodParameter = parameters[parameterIndex];

            if (methodParameter.GetCustomAttribute<InputAttribute>() != null)
            {
                var attribute = methodParameter.GetCustomAttribute<InputAttribute>();
                string inboundName = attribute.Name;

                if (string.IsNullOrEmpty(inboundName))
                {
                    throw new InvalidOperationException(StreamListenerErrorMessages.InvalidInboundName);
                }

                if (IsDeclarativeMethodParameter(inboundName, methodParameter))
                {
                    throw new InvalidOperationException(StreamListenerErrorMessages.InvalidDeclarativeMethodParameters);
                }

                return true;
            }

            if (methodParameter.GetCustomAttribute<OutputAttribute>() != null)
            {
                var attribute = methodParameter.GetCustomAttribute<OutputAttribute>();
                string outboundName = attribute.Name;

                if (string.IsNullOrEmpty(outboundName))
                {
                    throw new InvalidOperationException(StreamListenerErrorMessages.InvalidOutboundName);
                }

                if (IsDeclarativeMethodParameter(outboundName, methodParameter))
                {
                    throw new InvalidOperationException(StreamListenerErrorMessages.InvalidDeclarativeMethodParameters);
                }

                return true;
            }

            if (!string.IsNullOrEmpty(methodAnnotatedOutboundName))
            {
                return IsDeclarativeMethodParameter(methodAnnotatedOutboundName, methodParameter);
            }

            if (!string.IsNullOrEmpty(methodAnnotatedInboundName))
            {
                return IsDeclarativeMethodParameter(methodAnnotatedInboundName, methodParameter);
            }
        }

        return false;
    }

    private bool IsDeclarativeMethodParameter(string target, ParameterInfo methodParameter)
    {
        bool declarative = false;

        if (_context != null)
        {
            object targetBindable = BindingHelpers.GetBindableTarget(_context, target);

            if (!methodParameter.ParameterType.IsAssignableFrom(typeof(object)) && targetBindable != null)
            {
                declarative = typeof(IMessageChannel).IsAssignableFrom(methodParameter.ParameterType);

                if (!declarative)
                {
                    Type targetBeanClass = targetBindable.GetType();

                    foreach (IStreamListenerParameterAdapter adapter in _streamListenerParameterAdapters)
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

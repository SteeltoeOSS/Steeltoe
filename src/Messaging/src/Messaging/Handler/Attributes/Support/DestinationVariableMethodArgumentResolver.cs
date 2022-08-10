// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common.Converter;

namespace Steeltoe.Messaging.Handler.Attributes.Support;

public class DestinationVariableMethodArgumentResolver : AbstractNamedValueMethodArgumentResolver
{
    public const string DestinationTemplateVariablesHeader = $"{nameof(DestinationVariableMethodArgumentResolver)}.templateVariables";

    public DestinationVariableMethodArgumentResolver(IConversionService conversionService)
        : base(conversionService, null)
    {
    }

    public override bool SupportsParameter(ParameterInfo parameter)
    {
        return parameter.GetCustomAttribute<DestinationVariableAttribute>() != null;
    }

    protected override NamedValueInfo CreateNamedValueInfo(ParameterInfo parameter)
    {
        var annotation = parameter.GetCustomAttribute<DestinationVariableAttribute>();

        if (annotation == null)
        {
            throw new InvalidOperationException("No DestinationVariable annotation");
        }

        return new DestinationVariableNamedValueInfo(annotation);
    }

    protected override object ResolveArgumentInternal(ParameterInfo parameter, IMessage message, string name)
    {
        IMessageHeaders headers = message.Headers;
        headers.TryGetValue(DestinationTemplateVariablesHeader, out object obj);
        object result = null;

        if (obj is IDictionary<string, object> vars)
        {
            vars.TryGetValue(name, out result);
        }

        return result;
    }

    protected override void HandleMissingValue(string name, ParameterInfo parameter, IMessage message)
    {
        throw new MessageHandlingException(message, $"Missing path template variable '{name}' for method parameter type [{parameter.ParameterType}]");
    }

    protected class DestinationVariableNamedValueInfo : NamedValueInfo
    {
        public DestinationVariableNamedValueInfo(DestinationVariableAttribute annotation)
            : base(annotation.Name, true)
        {
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Converter;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Attributes.Support;

public class HeaderMethodArgumentResolver : AbstractNamedValueMethodArgumentResolver
{
    public HeaderMethodArgumentResolver(IConversionService conversionService, IApplicationContext context = null)
        : base(conversionService, context)
    {
    }

    public override bool SupportsParameter(ParameterInfo parameter)
    {
        return parameter.GetCustomAttribute<HeaderAttribute>() != null;
    }

    protected override NamedValueInfo CreateNamedValueInfo(ParameterInfo parameter)
    {
        var annotation = parameter.GetCustomAttribute<HeaderAttribute>();
        if (annotation == null)
        {
            throw new InvalidOperationException("No Header annotation");
        }

        return new HeaderNamedValueInfo(annotation);
    }

    protected override object ResolveArgumentInternal(ParameterInfo parameter, IMessage message, string name)
    {
        message.Headers.TryGetValue(name, out var headerValue);
        var nativeHeaderValue = GetNativeHeaderValue(message, name);

        if (headerValue != null && nativeHeaderValue != null)
        {
            // if (logger.isDebugEnabled())
            // {
            //    logger.debug("A value was found for '" + name + "', in both the top level header map " +
            //            "and also in the nested map for native headers. Using the value from top level map. " +
            //            "Use 'nativeHeader.myHeader' to resolve the native header.");
            // }
        }

        return headerValue ?? nativeHeaderValue;
    }

    protected override void HandleMissingValue(string headerName, ParameterInfo parameter, IMessage message)
    {
        throw new MessageHandlingException(message, $"Missing header '{headerName}' for method parameter type [{parameter.ParameterType}]");
    }

    private object GetNativeHeaderValue(IMessage message, string name)
    {
        var nativeHeaders = GetNativeHeaders(message);
        if (name.StartsWith("nativeHeaders."))
        {
            name = name.Substring("nativeHeaders.".Length);
        }

        if (nativeHeaders == null || !nativeHeaders.ContainsKey(name))
        {
            return null;
        }

        nativeHeaders.TryGetValue(name, out var nativeHeaderValues);
        if (nativeHeaderValues.Count == 1)
        {
            return nativeHeaderValues[0];
        }

        return nativeHeaderValues;
    }

    private IDictionary<string, List<string>> GetNativeHeaders(IMessage message)
    {
        message.Headers.TryGetValue(NativeMessageHeaderAccessor.NativeHeaders, out var result);
        return (IDictionary<string, List<string>>)result;
    }

    private sealed class HeaderNamedValueInfo : NamedValueInfo
    {
        public HeaderNamedValueInfo(HeaderAttribute annotation)
            : base(annotation.Name, annotation.Required, annotation.DefaultValue)
        {
        }
    }
}

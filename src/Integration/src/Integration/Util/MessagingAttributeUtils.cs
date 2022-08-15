// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Attributes;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Handler.Attributes;

namespace Steeltoe.Integration.Util;

public static class MessagingAttributeUtils
{
    public static bool HasValue(object value)
    {
        return value != null && (value is not string strVal || !string.IsNullOrEmpty(strVal)) && (!value.GetType().IsArray || ((Array)value).Length > 0);
    }

    public static T ResolveAttribute<T>(List<Attribute> attributes, string name)
    {
        foreach (Attribute attribute in attributes)
        {
            object value = AttributeUtils.GetValue(attribute, name);

            if (value != null && value.GetType() == typeof(T) && HasValue(value))
            {
                return (T)value;
            }
        }

        return default;
    }

    public static string EndpointIdValue(MethodInfo method)
    {
        var endpointId = method.GetCustomAttribute<EndpointIdAttribute>();
        return endpointId?.Id;
    }

    internal static object FindMessagePartAttribute(object[] attributes, bool payloads)
    {
        if (attributes == null || attributes.Length == 0)
        {
            return null;
        }

        object match = null;

        foreach (object annotation in attributes)
        {
            Type type = annotation.GetType();

            if (type == typeof(PayloadAttribute) || type == typeof(HeaderAttribute) || type == typeof(HeadersAttribute) ||
                (payloads && type == typeof(PayloadsAttribute)))
            {
                if (match != null)
                {
                    throw new MessagingException(
                        $"At most one parameter annotation can be provided for message mapping, but found two: [{match.GetType().Name}] and [{annotation.GetType().Name}]");
                }

                match = annotation;
            }
        }

        return match;
    }
}

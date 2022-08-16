// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Attributes;

namespace Steeltoe.Integration.Util;

public static class MessagingAttributeUtils
{
    public static bool HasValue(object value)
    {
        if (value == null)
        {
            return false;
        }

        if (value is string stringValue && string.IsNullOrEmpty(stringValue))
        {
            return false;
        }

        if (value.GetType().IsArray && ((Array)value).Length == 0)
        {
            return false;
        }

        return true;
    }

    public static T ResolveAttribute<T>(ICollection<Attribute> attributes, string name)
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
}

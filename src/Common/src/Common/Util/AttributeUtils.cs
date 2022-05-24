// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Common.Util
{
    public static class AttributeUtils
    {
        public static object GetValue(Attribute attribute, string propertyName)
        {
            if (attribute == null || string.IsNullOrEmpty(propertyName))
            {
                return null;
            }

            var property = attribute.GetType().GetProperty(propertyName);
            if (property != null)
            {
                return property.GetValue(attribute);
            }

            return null;
        }

        public static List<MethodInfo> FindMethodsWithAttribute(Type targetClass, Type attribute, BindingFlags flags)
        {
            var results = new List<MethodInfo>();
            var targetMethods = targetClass.GetMethods(flags);

            foreach (var method in targetMethods)
            {
                var attr = method.GetCustomAttribute(attribute);
                if (attr != null)
                {
                    results.Add(method);
                }
            }

            return results;
        }
    }
}

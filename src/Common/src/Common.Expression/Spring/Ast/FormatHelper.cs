// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public static class FormatHelper
    {
        public static string FormatMethodForMessage(string name, IList<Type> argumentTypes)
        {
            var items = new List<string>();
            foreach (var typeDescriptor in argumentTypes)
            {
                items.Add(typeDescriptor != null
                    ? FormatClassNameForMessage(typeDescriptor)
                    : FormatClassNameForMessage(null));
            }

            return name + "(" + string.Join(",", items) + ")";
        }

        public static string FormatClassNameForMessage(Type clazz)
        {
            return clazz != null ? clazz.FullName : "null";
        }
    }
}

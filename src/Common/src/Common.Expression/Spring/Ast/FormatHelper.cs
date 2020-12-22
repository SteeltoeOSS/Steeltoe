// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Common.Expression.Spring.Ast
{
    public static class FormatHelper
    {
        public static string FormatMethodForMessage(string name, IList<Type> argumentTypes)
        {
            var items = new List<string>();
            foreach (var typeDescriptor in argumentTypes)
            {
                if (typeDescriptor != null)
                {
                    items.Add(FormatClassNameForMessage(typeDescriptor));
                }
                else
                {
                    items.Add(FormatClassNameForMessage(null));
                }
            }

            return name + "(" + string.Join(",", items) + ")";
        }

        public static string FormatClassNameForMessage(Type clazz)
        {
            return clazz != null ? clazz.FullName : "null";
        }
    }
}

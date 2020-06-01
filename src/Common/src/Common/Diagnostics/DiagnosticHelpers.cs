// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Common.Diagnostics
{
    public static class DiagnosticHelpers
    {
        // TODO: Fix perf of this code
        public static T GetProperty<T>(object o, string name)
        {
            var property = o.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property == null)
            {
                return default(T);
            }

            return (T)property.GetValue(o);
        }
    }
}

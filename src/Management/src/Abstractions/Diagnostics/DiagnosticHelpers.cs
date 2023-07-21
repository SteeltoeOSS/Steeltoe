// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common;

namespace Steeltoe.Management.Diagnostics;

internal static class DiagnosticHelpers
{
    public static T GetPropertyOrDefault<T>(object instance, string name)
    {
        ArgumentGuard.NotNull(instance);
        ArgumentGuard.NotNull(name);

        PropertyInfo property = instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);

        if (property == null)
        {
            return default;
        }

        return (T)property.GetValue(instance);
    }
}

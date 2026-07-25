// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Build.Utilities;

namespace Steeltoe.Management.GitProperties.Build;

internal static class TaskExtensions
{
    // Only reachable when a real, unexpected I/O error occurs mid-build, which tests can't reliably induce.
    [ExcludeFromCodeCoverage]
    public static bool LogOnFailure(this Task task, string errorMessage, Action action)
    {
        return LogOnFailure(task, errorMessage, () =>
        {
            action();
            return true;
        });
    }

    // Only reachable when a real, unexpected I/O error occurs mid-build, which tests can't reliably induce.
    [ExcludeFromCodeCoverage]
    public static bool LogOnFailure(this Task task, string errorMessage, Func<bool> action)
    {
        try
        {
            return action();
        }
        catch (Exception exception)
        {
            task.Log.LogError($"git.properties: {errorMessage}:{Environment.NewLine}{exception}");
            return false;
        }
    }
}

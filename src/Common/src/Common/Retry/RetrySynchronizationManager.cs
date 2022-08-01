// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Retry;

public static class RetrySynchronizationManager
{
    private static readonly AsyncLocal<IRetryContext> Context = new ();

    public static IRetryContext GetContext()
    {
        return Context.Value;
    }

    public static IRetryContext Register(IRetryContext context)
    {
        var oldContext = GetContext();
        Context.Value = context;
        return oldContext;
    }

    public static IRetryContext Clear()
    {
        var value = GetContext();
        Context.Value = value?.Parent;
        return value;
    }
}

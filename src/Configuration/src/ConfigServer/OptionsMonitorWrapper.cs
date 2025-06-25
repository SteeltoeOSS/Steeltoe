// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;

namespace Steeltoe.Configuration.ConfigServer;

internal static class OptionsMonitorWrapper
{
    public static OptionsMonitorWrapper<T> Create<T>(T options)
        where T : notnull
    {
        return new OptionsMonitorWrapper<T>(options);
    }
}

internal sealed class OptionsMonitorWrapper<T> : IOptionsMonitor<T>
    where T : notnull
{
    public T CurrentValue { get; }

    public OptionsMonitorWrapper(T options)
    {
        ArgumentNullException.ThrowIfNull(options);

        CurrentValue = options;
    }

    public T Get(string? name)
    {
        return CurrentValue;
    }

    public IDisposable? OnChange(Action<T, string?> listener)
    {
        return null;
    }
}

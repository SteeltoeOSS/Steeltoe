// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;

namespace Steeltoe.Common.TestResources;

public static class TestOptionsMonitor
{
    public static TestOptionsMonitor<T> Create<T>(T options)
        where T : new()
    {
        return new TestOptionsMonitor<T>(options);
    }
}

public sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
    where T : new()
{
    private readonly List<Action<T, string?>> _listeners = [];

    public T CurrentValue { get; private set; }

    public TestOptionsMonitor()
    {
        CurrentValue = new T();
    }

    public TestOptionsMonitor(T options)
    {
        ArgumentNullException.ThrowIfNull(options);

        CurrentValue = options;
    }

    public T Get(string? name)
    {
        return CurrentValue;
    }

    public IDisposable OnChange(Action<T, string?> listener)
    {
        _listeners.Add(listener);
        return EmptyDisposable.Instance;
    }

    public void Change(T options)
    {
        CurrentValue = options;

        foreach (Action<T, string?> listener in _listeners)
        {
            listener(CurrentValue, string.Empty);
        }
    }
}

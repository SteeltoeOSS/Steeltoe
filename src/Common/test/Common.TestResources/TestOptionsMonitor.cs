// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;

namespace Steeltoe.Common.TestResources;

public static class TestOptionsMonitor
{
    public static TestOptionsMonitor<T> Create<T>(T currentValue)
        where T : new()
    {
        return new TestOptionsMonitor<T>(currentValue);
    }
}

public sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
    where T : new()
{
    public T CurrentValue { get; }

    public TestOptionsMonitor()
    {
        CurrentValue = new T();
    }

    public TestOptionsMonitor(T currentValue)
    {
        ArgumentNullException.ThrowIfNull(currentValue);

        CurrentValue = currentValue;
    }

    public T Get(string name)
    {
        return CurrentValue;
    }

    public IDisposable OnChange(Action<T, string> listener)
    {
        return EmptyDisposable.Instance;
    }
}

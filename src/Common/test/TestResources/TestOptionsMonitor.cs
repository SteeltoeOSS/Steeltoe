// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;

namespace Steeltoe.Common.TestResources;

public static class TestOptionsMonitor
{
    /// <summary>
    /// Creates an <see cref="IOptionsMonitor{TOptions}" /> from an existing options instance.
    /// </summary>
    /// <typeparam name="T">
    /// The options type.
    /// </typeparam>
    /// <param name="options">
    /// The options to wrap.
    /// </param>
    public static TestOptionsMonitor<T> Create<T>(T options)
        where T : new()
    {
        return new TestOptionsMonitor<T>(options);
    }
}

/// <summary>
/// Provides an implementation of <see cref="IOptionsMonitor{TOptions}" /> for testing.
/// </summary>
/// <typeparam name="T">
/// The options type.
/// </typeparam>
public sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
    where T : new()
{
    private readonly List<Action<T, string?>> _listeners = [];

    /// <summary>
    /// Gets the number of change listeners that are currently subscribed.
    /// </summary>
    public int ListenerCount
    {
        get
        {
            lock (_listeners)
            {
                return _listeners.Count;
            }
        }
    }

    public T CurrentValue { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TestOptionsMonitor{T}" /> class, creating a new options instance.
    /// </summary>
    public TestOptionsMonitor()
    {
        CurrentValue = new T();
    }

    // Use TestOptionsMonitor.Create() from tests, so the compiler infers the options type.
    internal TestOptionsMonitor(T options)
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
        ArgumentNullException.ThrowIfNull(listener);

        lock (_listeners)
        {
            _listeners.Add(listener);
        }

        return new Subscription(this, listener);
    }

    public void Change([DisallowNull] T options)
    {
        ArgumentNullException.ThrowIfNull(options);

        CurrentValue = options;

        Action<T, string?>[] listeners;

        lock (_listeners)
        {
            listeners = [.. _listeners];
        }

        foreach (Action<T, string?> listener in listeners)
        {
            listener(CurrentValue, string.Empty);
        }
    }

    private sealed class Subscription(TestOptionsMonitor<T> owner, Action<T, string?> listener) : IDisposable
    {
        public void Dispose()
        {
            lock (owner._listeners)
            {
                owner._listeners.Remove(listener);
            }
        }
    }
}

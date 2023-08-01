// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace Steeltoe.Management.Endpoint.Test.Trace;

internal sealed class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new(StringComparer.OrdinalIgnoreCase);

    public bool IsAvailable => true;
    public string Id => "TestSessionId";
    public IEnumerable<string> Keys => _store.Keys;

    public void Clear()
    {
        _store.Clear();
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task LoadAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void Remove(string key)
    {
        _store.Remove(key);
    }

    public void Set(string key, byte[] value)
    {
        _store[key] = value;
    }

    public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value)
    {
        return _store.TryGetValue(key, out value);
    }
}

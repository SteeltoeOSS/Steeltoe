// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.TestResources;

/// <summary>
/// Enables temporarily setting/changing an environment variable from a test. The original value is restored when disposed.
/// </summary>
public sealed class EnvironmentVariableScope : IDisposable
{
    private readonly string _name;
    private readonly string? _originalValue;

    public EnvironmentVariableScope(string name, string? value)
    {
        ArgumentNullException.ThrowIfNull(name);

        _name = name;
        _originalValue = Environment.GetEnvironmentVariable(name);

        if (_originalValue != value)
        {
            Environment.SetEnvironmentVariable(name, value);
        }
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(_name, _originalValue);
    }
}

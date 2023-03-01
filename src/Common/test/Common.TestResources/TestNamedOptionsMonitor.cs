// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Steeltoe.Common.TestResources;

public class TestNamedOptionsMonitor<T> : IOptionsMonitor<T>
{
    public T CurrentValue { get; }
    private readonly Dictionary<string, T> _namedValues = new();

    public TestNamedOptionsMonitor(params KeyValuePair<string,T>[] namedValues )
    {
        foreach ((var key,var value) in namedValues)
        {
            if (key == Options.DefaultName)
            {
                CurrentValue = value;
            }
            if (!_namedValues.ContainsKey(key))
            {
                _namedValues.Add(key, value);
            }
        }
            
    }

    public T Get(string name)
    {
        return _namedValues[name];
    }

    public IDisposable OnChange(Action<T, string> listener)
    {
        throw new NotImplementedException();
    }
}

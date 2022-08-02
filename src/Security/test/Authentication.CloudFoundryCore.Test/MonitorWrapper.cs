// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test;

public class MonitorWrapper<T> : IOptionsMonitor<T>
{
    public T CurrentValue { get; }

    public MonitorWrapper(T options)
    {
        CurrentValue = options;
    }

    public T Get(string name)
    {
        return CurrentValue;
    }

    public IDisposable OnChange(Action<T, string> listener)
    {
        throw new NotImplementedException();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using System;

namespace Steeltoe.Discovery.Eureka.Test
{
    public class TestOptionMonitorWrapper<T> : IOptionsMonitor<T>
    {
        public TestOptionMonitorWrapper(T opt)
        {
            CurrentValue = opt;
        }

        public T CurrentValue { get; private set; }

        public T Get(string name)
        {
            return CurrentValue;
        }

        public IDisposable OnChange(Action<T, string> listener)
        {
            throw new NotImplementedException();
        }
    }
}

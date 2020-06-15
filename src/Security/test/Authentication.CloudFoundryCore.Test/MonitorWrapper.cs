// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using System;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test
{
    public class MonitorWrapper<T> : IOptionsMonitor<T>
    {
        private T _options;

        public MonitorWrapper(T options)
        {
            _options = options;
        }

        public T CurrentValue
        {
            get
            {
                return _options;
            }
        }

        public T Get(string name)
        {
            return _options;
        }

        public IDisposable OnChange(Action<T, string> listener)
        {
            throw new NotImplementedException();
        }
    }
}

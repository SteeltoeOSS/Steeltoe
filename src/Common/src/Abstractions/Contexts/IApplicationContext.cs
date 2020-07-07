// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Contexts
{
    public interface IApplicationContext
    {
        IConfiguration Configuration { get; }

        IServiceProvider ServiceProvider { get; }

        object GetService(string name, Type serviceType);

        T GetService<T>(string name);

        T GetService<T>();

        object GetService(Type serviceType);

        IEnumerable<T> GetServices<T>();

        bool ContainsService(string name, Type serviceType);

        bool ContainsService<T>(string name);
    }
}

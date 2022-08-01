// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Expression.Internal.Contexts;

namespace Steeltoe.Common.Contexts;

public interface IApplicationContext : IDisposable
{
    IConfiguration Configuration { get; }

    IServiceProvider ServiceProvider { get; }

    IServiceExpressionResolver ServiceExpressionResolver { get; set; }

    object GetService(string name);

    object GetService(string name, Type serviceType);

    T GetService<T>(string name);

    T GetService<T>();

    object GetService(Type serviceType);

    IEnumerable<T> GetServices<T>();

    bool ContainsService(string name);

    bool ContainsService(string name, Type serviceType);

    bool ContainsService<T>(string name);

    void Register(string name, object instance);

    object Deregister(string name);

    string ResolveEmbeddedValue(string value);
}

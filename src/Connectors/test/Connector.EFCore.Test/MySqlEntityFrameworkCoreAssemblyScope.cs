// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Connector.EFCore.Test;

internal sealed class MySqlEntityFrameworkCoreAssemblyScope : IDisposable
{
    private readonly string[] _backupAssemblies;

    public MySqlEntityFrameworkCoreAssemblyScope(string[] assemblies)
    {
        _backupAssemblies = EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies;
        EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies = assemblies;
    }

    public void Dispose()
    {
        EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies = _backupAssemblies;
    }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common;
using Steeltoe.Connectors.RuntimeTypeAccess;

namespace Steeltoe.Connectors.CosmosDb.RuntimeTypeAccess;

internal sealed class CosmosClientShim : Shim, IDisposable
{
    public override IDisposable Instance => (IDisposable)base.Instance;

    private CosmosClientShim(InstanceAccessor instanceAccessor)
        : base(instanceAccessor)
    {
    }

    public static CosmosClientShim CreateInstance(CosmosDbPackageResolver packageResolver, string connectionString)
    {
        ArgumentGuard.NotNull(packageResolver);

        InstanceAccessor instanceAccessor = packageResolver.CosmosClientClass.CreateInstance(connectionString, null);
        return new CosmosClientShim(instanceAccessor);
    }

    public void Dispose()
    {
        Instance.Dispose();
    }
}

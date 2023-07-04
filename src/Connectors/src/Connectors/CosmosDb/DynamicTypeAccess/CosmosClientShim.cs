// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.CosmosDb.DynamicTypeAccess;

internal sealed class CosmosClientShim : Shim, IDisposable
{
    public override IDisposable Instance => (IDisposable)base.Instance;

    public CosmosClientShim(CosmosDbPackageResolver packageResolver, object instance)
        : this(new InstanceAccessor(packageResolver.CosmosClientClass, instance))
    {
    }

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

    public Task ReadAccountAsync()
    {
        return (Task)InstanceAccessor.InvokeMethod("ReadAccountAsync", true)!;
    }

    public void Dispose()
    {
        Instance.Dispose();
    }
}

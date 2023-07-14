// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.MongoDb.DynamicTypeAccess;

internal sealed class MongoClientInterfaceShim : Shim
{
    public MongoClientInterfaceShim(MongoDbPackageResolver packageResolver, object instance)
        : base(new InstanceAccessor(packageResolver.MongoClientInterface, instance))
    {
    }

    public IDisposable ListDatabaseNames(CancellationToken cancellationToken)
    {
        return (IDisposable)InstanceAccessor.InvokeMethodOverload("ListDatabaseNames", true, new[]
        {
            typeof(CancellationToken)
        }, cancellationToken)!;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.MongoDb.DynamicTypeAccess;

internal sealed class MongoClientInterfaceShim(MongoDbPackageResolver packageResolver, object instance)
    : Shim(new InstanceAccessor(packageResolver.MongoClientInterface, instance)), IDisposable
{
    public async Task<IDisposable> ListDatabaseNamesAsync(CancellationToken cancellationToken)
    {
        var task = (Task)InstanceAccessor.InvokeMethodOverload("ListDatabaseNamesAsync", true, [typeof(CancellationToken)], cancellationToken)!;

        await task;

        using var taskShim = new TaskShim<IDisposable>(task);
        return taskShim.GetResult();
    }

    public void Dispose()
    {
        // IMongoClient was changed in MongoDB.Driver v3.0 to implement IDisposable, so type-check for backward compatibility.
        if (Instance is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

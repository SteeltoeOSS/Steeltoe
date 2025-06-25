// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.MongoDb.DynamicTypeAccess;

internal sealed class MongoClientShim : Shim
{
    private MongoClientShim(InstanceAccessor instanceAccessor)
        : base(instanceAccessor)
    {
    }

    public static MongoClientShim CreateInstance(MongoDbPackageResolver packageResolver, string? connectionString)
    {
        ArgumentNullException.ThrowIfNull(packageResolver);

        // Ensure the argument is never null, so the proper ctor overload is selected (prevents AmbiguousMatchException).
        string constructorArgument = connectionString ?? string.Empty;

        InstanceAccessor instanceAccessor = packageResolver.MongoClientClass.CreateInstance(constructorArgument);
        return new MongoClientShim(instanceAccessor);
    }
}

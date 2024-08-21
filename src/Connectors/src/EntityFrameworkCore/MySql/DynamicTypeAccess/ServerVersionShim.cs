// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.EntityFrameworkCore.MySql.DynamicTypeAccess;

internal sealed class ServerVersionShim : Shim
{
    public ServerVersionShim(MySqlEntityFrameworkCorePackageResolver packageResolver, object instance)
        : base(new InstanceAccessor(packageResolver.ServerVersionClass, instance))
    {
    }

    public static ServerVersionShim AutoDetect(MySqlEntityFrameworkCorePackageResolver packageResolver, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(packageResolver);

        object instance = packageResolver.ServerVersionClass.InvokeMethodOverload("AutoDetect", true, [typeof(string)], connectionString)!;

        return new ServerVersionShim(packageResolver, instance);
    }
}

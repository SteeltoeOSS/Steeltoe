// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.Redis.DynamicTypeAccess;

internal sealed class ConnectionMultiplexerInterfaceShim : Shim
{
    public string ClientName => InstanceAccessor.GetPropertyValue<string>("ClientName");

    public ConnectionMultiplexerInterfaceShim(StackExchangeRedisPackageResolver packageResolver, object instance)
        : base(new InstanceAccessor(packageResolver.ConnectionMultiplexerInterface, instance))
    {
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.RabbitMQ.DynamicTypeAccess;

/// <summary>
/// Provides access to types in RabbitMQ NuGet packages, without referencing them.
/// </summary>
internal sealed class RabbitMQPackageResolver : PackageResolver
{
    public TypeAccessor ConnectionFactoryClass => ResolveType("RabbitMQ.Client.ConnectionFactory");
    public TypeAccessor ConnectionInterface => ResolveType("RabbitMQ.Client.IConnection");

    public RabbitMQPackageResolver()
        : base("RabbitMQ.Client", "RabbitMQ.Client")
    {
    }
}

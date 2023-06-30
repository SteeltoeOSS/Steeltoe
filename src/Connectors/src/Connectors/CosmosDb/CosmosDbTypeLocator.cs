// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Reflection;

// ReSharper disable once CheckNamespace
namespace Steeltoe.Connectors.CosmosDb;

/// <summary>
/// Assemblies and types used for interacting with CosmosDB.
/// </summary>
public static class CosmosDbTypeLocator
{
    /// <summary>
    /// Gets a list of supported CosmosDB assemblies.
    /// </summary>
    public static string[] Assemblies { get; internal set; } =
    {
        "Microsoft.Azure.Cosmos.Client"
    };

    public static string[] ClientOptionsTypeNames { get; internal set; } =
    {
        "Microsoft.Azure.Cosmos.CosmosClientOptions"
    };

    public static Type CosmosClientOptions => ReflectionHelpers.FindTypeOrThrow(Assemblies, ClientOptionsTypeNames, "CosmosClientOptions", "a CosmosDB client");
}

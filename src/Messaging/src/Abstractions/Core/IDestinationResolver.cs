// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.Core;

/// <summary>
/// Strategy for resolving a string name to a destination
/// </summary>
public interface IDestinationResolver
{
    /// <summary>
    /// Resolve the name to a destination
    /// </summary>
    /// <param name="name">the name to resolve</param>
    /// <returns>the destination if it exists</returns>
    object ResolveDestination(string name);
}

/// <summary>
/// A typed strategy for resolving a string name to a destination
/// </summary>
/// <typeparam name="T">the type of destinations this resolver returns</typeparam>
public interface IDestinationResolver<out T> : IDestinationResolver
{
    /// <summary>
    /// Resolve the name to a destination
    /// </summary>
    /// <param name="name">the name to resolve</param>
    /// <returns>the destination if it exists</returns>
    new T ResolveDestination(string name);
}
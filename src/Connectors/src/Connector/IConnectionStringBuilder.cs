// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Steeltoe.Connector;

/// <summary>
/// Provides an interface for connection string builders.
/// </summary>
internal interface IConnectionStringBuilder
{
    /// <summary>
    /// Gets or sets the connection string associated with this builder.
    /// </summary>
    /// <returns>
    /// The connection string, created from the key/value pairs that are contained within this builder. The default value is an empty string.
    /// </returns>
    [AllowNull]
    string ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the value associated with the specified key. Assigning a null value removes the key/value pair.
    /// </summary>
    /// <param name="keyword">
    /// The key of the item to get or set.
    /// </param>
    /// <returns>
    /// The value associated with the specified key, or <c>null</c> if not found. Throws an <see cref="ArgumentException" /> if the keyword is not supported.
    /// </returns>
    object? this[string keyword] { get; set; }
}

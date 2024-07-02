// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connectors.MongoDb;

/// <inheritdoc />
public sealed class MongoDbOptions : ConnectionStringOptions
{
    /// <summary>
    /// Gets or sets the name of the MongoDB database.
    /// </summary>
    public string? Database { get; set; }
}

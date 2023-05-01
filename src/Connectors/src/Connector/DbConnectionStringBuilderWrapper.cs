// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Steeltoe.Common;

namespace Steeltoe.Connector;

/// <summary>
/// Wraps an existing <see cref="DbConnectionStringBuilder" />.
/// </summary>
internal sealed class DbConnectionStringBuilderWrapper : IConnectionStringBuilder
{
    private readonly DbConnectionStringBuilder _innerBuilder;

    /// <inheritdoc />
    [AllowNull]
    public string ConnectionString
    {
        get => _innerBuilder.ConnectionString;
        set => _innerBuilder.ConnectionString = value;
    }

    /// <inheritdoc />
    public object? this[string keyword]
    {
        get => _innerBuilder[keyword];
        set => _innerBuilder[keyword] = value;
    }

    public DbConnectionStringBuilderWrapper(DbConnectionStringBuilder innerBuilder)
    {
        ArgumentGuard.NotNull(innerBuilder);
        _innerBuilder = innerBuilder;
    }
}

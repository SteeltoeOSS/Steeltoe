// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Common.Options;

/// <summary>
/// Base type representing ASP.NET configuration options.
/// </summary>
/// <remarks>
/// Binds with an <see cref="IConfiguration" /> when instantiated.
/// </remarks>
public abstract class AbstractOptions
{
    // This constructor is for use with IOptions.
    protected AbstractOptions()
    {
    }

    protected AbstractOptions(IConfiguration root)
        : this(root, null)
    {
    }

    protected AbstractOptions(IConfiguration root, string sectionPrefix)
    {
        ArgumentGuard.NotNull(root);

        IConfiguration section = string.IsNullOrEmpty(sectionPrefix) ? root : root.GetSection(sectionPrefix);
        section.Bind(this);
    }
}

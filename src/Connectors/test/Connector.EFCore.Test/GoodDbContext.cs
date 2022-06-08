// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;

namespace Steeltoe.Connector.EFCore.Test;

internal class GoodDbContext : DbContext
{
    public GoodDbContext(DbContextOptions<GoodDbContext> options)
        : base(options)
    {
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.DbMigrations;

public class DbMigrationsDescriptor
{
    public IList<string> PendingMigrations { get; set; } = new List<string>();

    public IList<string> AppliedMigrations { get; set; } = new List<string>();
}

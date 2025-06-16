// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Management.Endpoint.Actuators.DbMigrations;

internal interface IDatabaseMigrationScanner
{
    Assembly AssemblyToScan { get; }

    /// <summary>
    /// Gets all migrations that are defined in the assembly but haven't been applied to the target database.
    /// </summary>
    IEnumerable<string> GetPendingMigrations(object context);

    /// <summary>
    /// Gets all migrations that have been applied to the target database.
    /// </summary>
    IEnumerable<string> GetAppliedMigrations(object context);

    /// <summary>
    /// Gets all the migrations that are defined in the configured migrations assembly.
    /// </summary>
    IEnumerable<string> GetMigrations(object context);
}

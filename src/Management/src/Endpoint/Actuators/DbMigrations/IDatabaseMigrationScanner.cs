// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Management.Endpoint.Actuators.DbMigrations;

internal interface IDatabaseMigrationScanner
{
    Assembly AssemblyToScan { get; }

    IEnumerable<string> GetPendingMigrations(object context);

    IEnumerable<string> GetAppliedMigrations(object context);

    IEnumerable<string> GetMigrations(object context);
}

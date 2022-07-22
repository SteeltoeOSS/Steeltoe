// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Entity;

namespace Steeltoe.Connector.Oracle.EF6.Test;

public class GoodOracleDbContext : DbContext
{
    public GoodOracleDbContext(string str)
    {
    }
}
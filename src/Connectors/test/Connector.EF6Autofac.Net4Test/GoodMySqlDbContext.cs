// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using MySql.Data.EntityFramework;
using System.Data.Entity;

namespace Steeltoe.CloudFoundry.Connector.EF6Autofac.Test
{
    [DbConfigurationType(typeof(MySqlEFConfiguration))]
    public class GoodMySqlDbContext : DbContext
    {
        public GoodMySqlDbContext(string str)
        {
        }
    }
}

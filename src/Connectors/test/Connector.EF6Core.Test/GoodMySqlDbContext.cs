﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET461
using MySql.Data.EntityFramework;
using System.Data.Entity;

namespace Steeltoe.Connector.MySql.EF6.Test
{
    [DbConfigurationType(typeof(MySqlEFConfiguration))]
    public class GoodMySqlDbContext : DbContext
    {
        public GoodMySqlDbContext(string str)
        {
        }
    }
}
#endif
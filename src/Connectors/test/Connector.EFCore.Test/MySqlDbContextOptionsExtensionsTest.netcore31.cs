// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NETCOREAPP3_1
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Connector.EFCore.Test;

namespace Steeltoe.Connector.MySql.EFCore.Test;

public partial class MySqlDbContextOptionsExtensionsTest
{
    private static void AddMySqlDbContext(IServiceCollection services, IConfigurationRoot config, string serviceName = null)
    {
        if (serviceName == null)
        {
            services.AddDbContext<GoodDbContext>(options => options.UseMySql(config));
        }
        else
        {
            services.AddDbContext<GoodDbContext>(options => options.UseMySql(config, serviceName));
        }
    }
}
#endif

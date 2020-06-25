// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Steeltoe.Management.Endpoint.CloudFoundry;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Steeltoe.Management.Endpoint.Test
{
    public static class TestHelpers
    {
        public static IEnumerable<IManagementOptions> GetManagementOptions(params IEndpointOptions[] options)
        {
            var mgmtOptions = new CloudFoundryManagementOptions();
            mgmtOptions.EndpointOptions.AddRange(options);
            return new List<IManagementOptions>() { mgmtOptions };
        }

        public static IHostBuilder GetTestHost()
        {
            return new HostBuilder().ConfigureWebHost(builder => builder.UseTestServer().ConfigureServices(s => s.AddRouting()).Configure(a => a.UseRouting()));
        }
    }
}
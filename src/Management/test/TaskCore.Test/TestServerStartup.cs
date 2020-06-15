// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Steeltoe.Management.TaskCore.Test
{
    public class TestServerStartup
    {
        public TestServerStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTask("test", _ => throw new PassException());
        }

        public void Configure(IApplicationBuilder app)
        {
        }
    }

#pragma warning disable CA1032 // Implement standard exception constructors
#pragma warning disable SA1402 // File may only contain a single type
    internal class PassException : Exception
#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore CA1032 // Implement standard exception constructors
    {
    }
}

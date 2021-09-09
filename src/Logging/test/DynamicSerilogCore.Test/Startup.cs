// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Extensions.Logging.DynamicSerilog.Test
{
    public class Startup
    {
        private readonly ILogger<Startup> _logger;

        public Startup(ILogger<Startup> logger, IConfiguration configuration)
        {
            _logger = logger;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            _logger.LogError("error");
            _logger.LogInformation("info");
        }

        public void Configure(IApplicationBuilder app)
        {
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.CircuitBreaker.Hystrix.Test;
using Steeltoe.Common.Utils.IO;
using System.IO;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsStream.Test;

public class HystrixMetricsStreamOptionsTest : HystrixTestBase
{
    [Fact]
    public void Configure_SetsProperties()
    {
        var json = @"
                {
                    ""hystrix"" : {
                        ""stream"": {
                            ""validate_certificates"" : false
                        }
                    }
                }";
        using var sandbox = new Sandbox();
        var path = sandbox.CreateFile("appsettings.json", json);
        var directory = Path.GetDirectoryName(path);
        var fileName = Path.GetFileName(path);
        var builder = new ConfigurationBuilder();
        builder.SetBasePath(directory);
        builder.AddJsonFile(fileName);
        IConfiguration config = builder.Build();

        IServiceCollection services = new ServiceCollection();
        services.AddOptions();
        services.Configure<HystrixMetricsStreamOptions>(config.GetSection("hystrix:stream"));
        var provider = services.BuildServiceProvider();

        var options = provider.GetService<IOptions<HystrixMetricsStreamOptions>>();
        Assert.NotNull(options);
        var opts = options.Value;
        Assert.NotNull(opts);
        Assert.False(opts.ValidateCertificates);
        Assert.Equal(500, opts.SendRate);
        Assert.Equal(500, opts.GatherRate);
    }
}

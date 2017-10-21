//
// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Options.Autofac;
using System.IO;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsStream.Test
{
    public class HystrixMetricsStreamOptionsTest : HystrixTestBase
    {

        [Fact]
        public void Configure_SetsProperties()
        {
            var json = @"
{
    'hystrix' : {
        'stream': {
            'validate_certificates' : false
        }
    }
}
";
            string path = TestHelpers.CreateTempFile(json);
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.SetBasePath(directory);
            builder.AddJsonFile(fileName);
            IConfiguration config = builder.Build();

            ContainerBuilder services = new ContainerBuilder();
            services.RegisterOptions();
            services.RegisterOption<HystrixMetricsStreamOptions>(config.GetSection("hystrix:stream"));
            var provider = services.Build();

            var options = provider.Resolve<IOptions<HystrixMetricsStreamOptions>>();
            Assert.NotNull(options);
            var opts = options.Value;
            Assert.NotNull(opts);
            Assert.False(opts.Validate_Certificates);
            Assert.Equal(500, opts.SendRate);
            Assert.Equal(500, opts.GatherRate);
        }
    }
}

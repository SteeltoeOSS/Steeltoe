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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Info.Contributor;
using System.IO;
using Xunit;
using System;

namespace Steeltoe.Management.Endpoint.Info.Test
{
    public class EndpointServiceCollectionTest
    {

        [Fact]
        public void AddInfoActuator_AddsCorrectServices()
        {
            ServiceCollection services = new ServiceCollection();
            var appsettings = @"
{
    'management': {
        'endpoints': {
            'enabled': false,
            'sensitive': false,
            'path': '/management',
            'info' : {
                'enabled': false,
                'sensitive': false,
                'id': 'infomanagement'
            }
        }
    }
}";
            var path = TestHelpers.CreateTempFile(appsettings);
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddJsonFile(fileName);
            var config = configurationBuilder.Build();

            services.AddInfoActuator(config);

            ILogger<InfoEndpoint> logger = new TestLogger();
            services.AddSingleton<ILogger<InfoEndpoint>>(logger);

            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetService<IInfoOptions>();
            Assert.NotNull(options);
            var contribs = serviceProvider.GetServices<IInfoContributor>();
            Assert.NotNull(contribs);
            Assert.Contains(contribs, (item) => { return item.GetType() == typeof(GitInfoContributor) || item.GetType() == typeof(AppSettingsInfoContributor); });
            var ep = serviceProvider.GetService<InfoEndpoint>();
            Assert.NotNull(ep);
        }

    }
    class TestLogger : ILogger<InfoEndpoint>
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            throw new NotImplementedException();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            throw new NotImplementedException();
        }
    }
}

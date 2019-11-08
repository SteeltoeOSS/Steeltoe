// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Xunit;

namespace Steeltoe.Management.TaskCore.Test
{
    public class TaskRunTest
    {
        [Fact]
        public void DelegatingTask_WebHost_ExecutesRun()
        {
            var args = new[] { "runtask=test" };

            Assert.Throws<PassException>(() =>
                WebHost.CreateDefaultBuilder(args)
                    .UseStartup<TestStartup>()
                    .Build()
                    .RunWithTasks());
        }

#if NETCOREAPP3_0
        [Fact]
        public void DelegatingTask_GenericHost_ExecutesRun()
        {
            var args = new[] { "runtask=test" };

            Assert.Throws<PassException>(() =>
                Host.CreateDefaultBuilder(args)
                    .ConfigureWebHost(configure => configure.UseStartup<TestStartup>())
                    .Build()
                    .RunWithTasks());
        }
#endif

        public class TestStartup
        {
            public TestStartup(IConfiguration configuration)
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

        internal class PassException : Exception
        {
        }
    }
}
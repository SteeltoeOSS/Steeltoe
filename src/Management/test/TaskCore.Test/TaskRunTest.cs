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
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
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
                    .UseStartup<TestServerStartup>()
                    .Build()
                    .RunWithTasks());
        }

        [Fact]
        public void DelegatingTask_WebHost_StopsIfNoTask()
        {
            var args = new[] { "runtask=test" };

            WebHost.CreateDefaultBuilder(args)
                .Configure(c => { })
                .Build()
                .RunWithTasks();

            Assert.True(true, "If we reached this assertion, the app stopped without throwing anything");
        }

#if NETCOREAPP3_0
        [Fact]
        public void DelegatingTask_GenericHost_ExecutesRun()
        {
            var args = new[] { "runtask=test" };

            Assert.Throws<PassException>(() =>
                Host.CreateDefaultBuilder(args)
                    .ConfigureWebHost(configure => configure.UseStartup<TestServerStartup>().UseKestrel())
                    .Build()
                    .RunWithTasks());
        }

        [Fact]
        public void DelegatingTask_GenericHost_StopsIfNoTask()
        {
            var args = new[] { "runtask=test" };

            Host.CreateDefaultBuilder(args)
                .Build()
                .RunWithTasks();

            Assert.True(true, "If we reached this assertion, the app stopped without throwing anything");
        }
#endif
    }
}
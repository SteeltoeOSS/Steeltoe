// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

#if NETCOREAPP3_1
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
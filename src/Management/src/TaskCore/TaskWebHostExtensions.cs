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

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using System;
using System.Linq;

namespace Steeltoe.Management.TaskCore
{
    public static class TaskWebHostExtensions
    {
        /// <summary>
        /// Runs a web application, blocking the calling thread until the host shuts down.<para />
        /// To execute your task, invoke the application with argument "runtask=taskname", where "taskname" is your task's name.<para/>
        /// Command line arguments should be registered as a configuration source for this functionality to work.
        /// </summary>
        /// <param name="webHost">The <see cref="T:Microsoft.AspNetCore.Hosting.IWebHost" /> to run.</param>
        public static void RunWithTasks(this IWebHost webHost)
        {
            if (webHost == null)
            {
                throw new ArgumentNullException(nameof(webHost));
            }

            if (FindAndRunTask(webHost.Services))
            {
                webHost.Dispose();
            }
            else
            {
                webHost.Run();
            }
        }

        /// <summary>
        /// Runs a web application, blocking the calling thread until the host shuts down.<para />
        /// To execute your task, invoke the application with argument "runtask=taskname", where "taskname" is your task's name.<para/>
        /// Command line arguments should be registered as a configuration source for this functionality to work.
        /// </summary>
        /// <param name="host">The <see cref="T:Microsoft.AspNetCore.Hosting.IWebHost" /> to run.</param>
        public static void RunWithTasks(this IHost host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (FindAndRunTask(host.Services))
            {
                host.Dispose();
            }
            else
            {
                host.Run();
            }
        }

        private static bool FindAndRunTask(IServiceProvider services)
        {
            var config = services.GetRequiredService<IConfiguration>();
            var taskName = config.GetValue<string>("runtask");
            var scope = services.CreateScope().ServiceProvider;
            if (taskName != null)
            {
                var task = scope.GetServices<IApplicationTask>().FirstOrDefault(x => x.Name.ToLower() == taskName.ToLower());
                if (task != null)
                {
                    task.Run();
                }
                else
                {
                    var logger = scope.GetService<ILoggerFactory>()
                        .CreateLogger("CloudFoundryTasks");
                    logger.LogError($"No task with name {taskName} is found registered in service container");
                }

                return true;
            }

            return false;
        }
    }
}
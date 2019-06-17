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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Tasks;
using System;
using System.Linq;

namespace Microsoft.AspNetCore.Hosting
{
    public static class TaskWebHostExtensions
    {
        /// <summary>
        /// Runs your application normally unless configured to run a task.<para />
        /// To execute your task, provide a config value (or command arg) under the key "runtask" that matches your task's name
        /// </summary>
        /// <param name="webHost">Your <see cref="IWebHost"/></param>
        public static void RunWithTasks(this IWebHost webHost)
        {
            if (webHost == null)
            {
                throw new ArgumentNullException(nameof(webHost));
            }

            var config = webHost.Services.GetRequiredService<IConfiguration>();
            var taskName = config.GetValue<string>("runtask");
            var scope = webHost.Services.CreateScope().ServiceProvider;

            if (taskName != null)
            {
                var task = scope.GetServices<IApplicationTask>().FirstOrDefault(x => x.Name.ToLower() == taskName.ToLower());
                if (task != null)
                {
                    task.Run();
                }
                else
                {
                    var logger = scope.GetService<ILogger>();
                    logger.LogError($"No task with name {taskName} is found registered in service container");
                }
            }
            else
            {
                webHost.Run();
            }
        }
    }
}
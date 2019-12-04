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

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;
using System;

namespace Steeltoe.Management.TaskCore
{
    public static class TaskServiceCollectionExtensions
    {
        /// <summary>
        /// Register a one-off task that can be executed from command line
        /// </summary>
        /// <param name="services">Service container</param>
        /// <param name="lifetime">Task lifetime</param>
        /// <typeparam name="T">Task implementation</typeparam>
        public static void AddTask<T>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where T : class, IApplicationTask
        {
            services.Add(new ServiceDescriptor(typeof(IApplicationTask), typeof(T), lifetime));
        }

        /// <summary>
        /// Register a one-off task that can be executed from command line
        /// </summary>
        /// <param name="services">Service container</param>
        /// <param name="task">Task instance</param>
        public static void AddTask(this IServiceCollection services, IApplicationTask task)
        {
            services.Add(new ServiceDescriptor(typeof(IApplicationTask), task));
        }

        /// <summary>
        /// Register a one-off task that can be executed from command line
        /// </summary>
        /// <param name="services">Service container</param>
        /// <param name="factory">A factory method to create an application task</param>
        /// <param name="lifetime">Task lifetime</param>
        public static void AddTask(this IServiceCollection services, Func<IServiceProvider, IApplicationTask> factory, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            services.Add(new ServiceDescriptor(typeof(IApplicationTask), factory, lifetime));
        }

        /// <summary>
        /// Register a one-off task that can be executed from command line
        /// </summary>
        /// <param name="services">Service container</param>
        /// <param name="name">Well known name of the task. This is how it's identified when called</param>
        /// <param name="runAction">Task method body</param>
        /// <param name="lifetime">Task lifetime</param>
        public static void AddTask(this IServiceCollection services, string name, Action<IServiceProvider> runAction, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            services.Add(new ServiceDescriptor(typeof(IApplicationTask), svc => new DelegatingTask(name, () => runAction(svc)), lifetime));
        }
    }
}

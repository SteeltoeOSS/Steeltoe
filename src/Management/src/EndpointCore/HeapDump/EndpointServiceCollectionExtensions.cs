﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Hypermedia;
using System;
using System.Runtime.InteropServices;

namespace Steeltoe.Management.Endpoint.HeapDump
{
    public static class EndpointServiceCollectionExtensions
    {
        public static bool IsHeapDumpSupported()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && typeof(object).Assembly.GetType("System.Index") != null);
        }

        /// <summary>
        /// Adds components of the Heap Dump actuator to Microsoft-DI
        /// </summary>
        /// <param name="services">Service collection to add actuator to</param>
        /// <param name="config">Application configuration (this actuator looks for settings starting with management:endpoints:dump)</param>
        public static void AddHeapDumpActuator(this IServiceCollection services, IConfiguration config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (IsHeapDumpSupported())
            {
                services.AddActuatorManagementOptions(config);

                var options = new HeapDumpEndpointOptions(config);
                services.TryAddSingleton<IHeapDumpOptions>(options);
                services.RegisterEndpointOptions(options);

                if (Platform.IsWindows)
                {
                    services.TryAddSingleton<IHeapDumper, WindowsHeapDumper>();
                }
                else if (Platform.IsLinux)
                {
                    services.TryAddSingleton<IHeapDumper, LinuxHeapDumper>();
                }

                services.TryAddSingleton<HeapDumpEndpoint>();
            }
        }
    }
}

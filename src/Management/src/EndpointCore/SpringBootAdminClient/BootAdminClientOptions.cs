// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient
{
    public class BootAdminClientOptions
    {
        private const string PREFIX = "spring:boot:admin:client";
        private const string SPRING_APPLICATION_NAME = "spring:application:name";
        private const string DefaultAppName = "SteeltoeApp";
        private const string URLS = "URLS";

        public string Url { get; set; }

        public string ApplicationName { get; set; }

        public string BasePath { get; set; }

        public BootAdminClientOptions(IConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var section = config.GetSection(PREFIX);
            if (section != null)
            {
                section.Bind(this);
            }

            BasePath ??= GetBasePath(config);
            ApplicationName ??= config.GetValue<string>(SPRING_APPLICATION_NAME);
            ApplicationName ??= Assembly.GetEntryAssembly()?.GetName().Name ?? DefaultAppName;
        }

        private string GetBasePath(IConfiguration config)
        {
            var urlString = config.GetValue<string>(URLS);
            var urls = urlString?.Split(';');
            if (urls?.Length > 0)
            {
                return urls[0];
            }

            return null;
        }
    }
}

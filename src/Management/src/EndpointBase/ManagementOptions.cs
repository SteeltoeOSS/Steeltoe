// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint
{
    [Obsolete("Use ManagementEndpointOptions instead")]
    public class ManagementOptions : IManagementOptions
    {
        private const string DEFAULT_PATH = "/";
        private const string MANAGEMENT_INFO_PREFIX = "management:endpoints";
        private static ManagementOptions _instance;

        public ManagementOptions()
        {
            Path = DEFAULT_PATH;
            EndpointOptions = new List<IEndpointOptions>();
        }

        public ManagementOptions(IConfiguration config)
            : this()
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var section = config.GetSection(MANAGEMENT_INFO_PREFIX);
            if (section != null)
            {
                section.Bind(this);
            }
        }

        public bool? Enabled { get; set; }

        public bool? Sensitive { get; set; }

        public string Path { get; set; }

        public List<IEndpointOptions> EndpointOptions { get; set; }

        public bool UseStatusCodeFromResponse { get; set; } = true;

        public static ManagementOptions GetInstance()
        {
            if (_instance == null)
            {
                _instance = new ManagementOptions();
            }

            return _instance;
        }

        public static ManagementOptions GetInstance(IConfiguration config)
        {
            if (_instance == null)
            {
                _instance = new ManagementOptions(config);
            }

            return _instance;
        }

        public static void SetInstance(ManagementOptions mgmtOptions)
        {
            _instance = mgmtOptions;
        }

        public static void Reset()
        {
            _instance = null;
        }
    }
}

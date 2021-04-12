// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Steeltoe.Extensions.Configuration.SpringBootEnv
{
    /// <summary>
    /// Configuration provider that expands spring style '.' delimited configuration keys to .NET compatible format
    /// </summary>
    public class SpringBootEnvProvider : ConfigurationProvider
    {
        internal ILogger<SpringBootEnvProvider> _logger;

        private const string SPRING_APPLICATION_JSON = "SPRING_APPLICATION_JSON";

        internal string SpringApplicationJson { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpringBootEnvProvider"/> class.
        /// The new placeholder resolver wraps the provided configuration
        /// </summary>
        /// <param name="logFactory">the logger factory to use</param>
        public SpringBootEnvProvider(ILoggerFactory logFactory = null)
        {
            _logger = logFactory?.CreateLogger<SpringBootEnvProvider>();
            SpringApplicationJson = Environment.GetEnvironmentVariable(SPRING_APPLICATION_JSON);
        }

        /// <summary>
        /// Expands and loads the new keys
        /// </summary>
        public override void Load()
        {

            if (!string.IsNullOrEmpty(SpringApplicationJson))
            {
                var builder = new ConfigurationBuilder();
                builder.Add(new JsonStreamConfigurationSource()
                {
                    Stream = GetMemoryStream(SpringApplicationJson)
                });

                var servicesData = builder.Build();
                if (servicesData != null)
                {
                    foreach (var child in servicesData.GetChildren())
                    {
                        if (child.Key.Contains('.') && child.Value != null)
                        {
                            var nk = child.Key.Replace('.', ':');
                            Data[nk] = child.Value;
                        }

                        RExpand(child);
                    }
                }
            }
        }

        internal static MemoryStream GetMemoryStream(string json)
        {
            var memStream = new MemoryStream();
            var textWriter = new StreamWriter(memStream);
            textWriter.Write(json);
            textWriter.Flush();
            memStream.Seek(0, SeekOrigin.Begin);
            return memStream;
        }

        private void RExpand(IConfigurationSection section)
        {

            foreach (var child in section.GetChildren())
            {
                if (child.Key.Contains('.') && child.Value != null)
                {
                    var nk = child.Path.Replace('.', ':');
                    Data[nk] = child.Value;
                }

                RExpand(child);
            }
        }
    }
}

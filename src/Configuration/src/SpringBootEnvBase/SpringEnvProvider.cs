// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Extensions.Configuration.SpringBootEnv
{
    /// <summary>
    /// Configuration provider that expands spring style '.' delimited configuration keys to .NET compatible format
    /// </summary>
    public class SpringEnvProvider : ConfigurationProvider
    {
        internal ILogger<SpringEnvProvider> _logger;

        private const string SPRING_APPLICATION_JSON = "SPRING_APPLICATION_JSON";

        internal string SpringEnvJson { get; set; } = Environment.GetEnvironmentVariable(SPRING_APPLICATION_JSON);

        /// <summary>
        /// Initializes a new instance of the <see cref="SpringEnvProvider"/> class.
        /// The new placeholder resolver wraps the provided configuration
        /// </summary>
        /// <param name="logFactory">the logger factory to use</param>
        public SpringEnvProvider(ILoggerFactory logFactory = null)
        {
            _logger = logFactory?.CreateLogger<SpringEnvProvider>();
        }

        /// <summary>
        /// Expands and loads the new keys
        /// </summary>
        public override void Load()
        {
            if (!string.IsNullOrEmpty(SpringEnvJson))
            {
                var memStream = GetMemoryStream(SpringEnvJson);
                var builder = new ConfigurationBuilder();
                builder.Add(new JsonStreamConfigurationSource(memStream));
                IConfigurationRoot servicesData = builder.Build();
                var expanded = new Dictionary<string, object>();
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
    }
}

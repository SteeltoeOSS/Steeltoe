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
using Steeltoe.Connector.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Connector.GemFire
{
    public class GemFireConnectorOptions : AbstractServiceConnectorOptions
    {
        private const string GEMFIRE_CLIENT_SECTION_PREFIX = "gemfire:client";

        public GemFireConnectorOptions()
        {
        }

        public GemFireConnectorOptions(IConfiguration config)
            : base()
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            // bind most of the gemfire:client section
            var section = config.GetSection(GEMFIRE_CLIENT_SECTION_PREFIX);
            section.Bind(this);

            // call bind again to populate the locator list
            section.GetSection("locators").Bind(Locators);
            if (!Locators.Any())
            {
                Locators.Add("localhost[10334]");
            }
        }

        public string Username { get; set; } = Users?.FirstOrDefault(u => u.Roles.Contains("developer")).Username;

        public string Password { get; set; } = Users?.FirstOrDefault(u => u.Roles.Contains("developer")).Password;

        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        public Dictionary<string, int> ParsedLocators()
        {
            var toReturn = new Dictionary<string, int>();
            foreach (var locator in Locators)
            {
                if (locator.Contains('[') && locator.Contains(']'))
                {
                    // server[port]
                    var portStart = locator.IndexOf('[');
                    var portEnd = locator.IndexOf(']');
                    if (int.TryParse(locator.Substring(portStart + 1, portEnd - portStart - 1), out int parsedPort))
                    {
                        toReturn.Add(locator.Substring(0, portStart), parsedPort);
                    }
                    else
                    {
                        throw new ConnectorException("Non-numeric port value provided for locator");
                    }
                }
                else if (locator.Contains(':') && locator.Count(c => c.Equals(':')) == 1)
                {
                    // server:port
                    var serverPort = locator.Split(':');
                    if (int.TryParse(serverPort[1], out int parsedPort))
                    {
                        toReturn.Add(serverPort[0], parsedPort);
                    }
                    else
                    {
                        throw new ConnectorException("Non-numeric port value provided for locator");
                    }
                }
                else
                {
                    throw new ConnectorException($"Locator format unknown: {locator}");
                }
            }

            return toReturn;
        }

        internal static List<GemFireUser> Users { get; set; }

        /// <summary>
        /// Gets or sets Locators as a list formatted ServerNameOrAddress[PortNumber] (as supplied by the Pivotal Cloud Cache tile)
        /// </summary>
        internal List<string> Locators { get; set; } = new List<string>();
    }
}

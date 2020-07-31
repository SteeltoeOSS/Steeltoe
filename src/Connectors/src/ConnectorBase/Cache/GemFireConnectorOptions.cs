// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.CloudFoundry.Connector.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.CloudFoundry.Connector.GemFire
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
                    if (int.TryParse(locator.Substring(portStart + 1, portEnd - portStart - 1), out var parsedPort))
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
                    if (int.TryParse(serverPort[1], out var parsedPort))
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

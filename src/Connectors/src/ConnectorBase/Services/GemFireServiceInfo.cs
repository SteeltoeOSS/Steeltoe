// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.CloudFoundry.Connector.Services
{
    public class GemFireServiceInfo : ServiceInfo
    {
        public GemFireServiceInfo(string id)
            : base(id, null)
        {
        }

        public List<string> Locators { get; set; } = new List<string>();

        public Dictionary<string, string> Urls { get; set; } = new Dictionary<string, string>();

        public List<GemFireUser> Users { get; set; } = new List<GemFireUser>();
    }

#pragma warning disable SA1402 // File may only contain a single class
    public class GemFireUser
    {
        public string Password { get; set; }

        public IEnumerable<string> Roles { get; set; }

        public string Username { get; set; }
    }
#pragma warning restore SA1402 // File may only contain a single class
}

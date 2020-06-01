// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Steeltoe.Management.Endpoint.Security;
using System.Net;

namespace Steeltoe.Management.Endpoint.CloudFoundry
{
    public class SecurityResult
    {
        [JsonIgnore]
        public HttpStatusCode Code;

        [JsonIgnore]
        public Permissions Permissions;

        [JsonProperty("security_error")]
        public string Message;

        public SecurityResult(Permissions level)
        {
            Code = HttpStatusCode.OK;
            Message = string.Empty;
            Permissions = level;
        }

        public SecurityResult(HttpStatusCode code, string message)
        {
            Code = code;
            Message = message;
            Permissions = Permissions.NONE;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Security.Authentication.CloudFoundry.Owin
{
    /// <summary>
    /// Response object for /check_token endpoint
    /// </summary>
    public class CheckTokenResponse
    {
        public string User_id { get; set; }

        public string User_name { get; set; }

        public string Email { get; set; }

        public string Client_id { get; set; }

        public int Exp { get; set; }

        public List<string> Scope { get; set; }

        public string Jti { get; set; }

        public List<string> Aud { get; set; }

        public string Sub { get; set; }

        public string Iss { get; set; }

        public int Iat { get; set; }

        public string Cid { get; set; }

        public string Grant_type { get; set; }

        public string Azp { get; set; }

        public int Auth_time { get; set; }

        public string Zid { get; set; }

        public string Rev_sig { get; set; }

        public string Origin { get; set; }

        public bool Revocable { get; set; }
    }
}

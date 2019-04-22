// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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

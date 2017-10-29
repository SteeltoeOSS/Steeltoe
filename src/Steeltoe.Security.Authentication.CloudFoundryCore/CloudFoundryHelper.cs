//
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
//

using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http;
using Steeltoe.Common;

namespace Steeltoe.Security.Authentication.CloudFoundry
{

    public static class CloudFoundryHelper
    {
        public static DateTime GetIssueTime(JObject payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }
            var time = payload.Value<long>("iat");
            return ToAbsoluteUTC(time);
        }

        public static DateTime GetExpTime(JObject payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }
            var time = payload.Value<long>("exp");
            return ToAbsoluteUTC(time);
        }

        private static DateTime baseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static DateTime ToAbsoluteUTC(long secondsPastEpoch)
        {
            return baseTime.AddSeconds(secondsPastEpoch);
        }

        public static List<string> GetScopes(JObject user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            List<string> result = new List<string>();
            var scopes = user["scope"];
            if (scopes == null)
            {
                return result;
            }

            var asValue = scopes as JValue;
            if (asValue != null)
            {
                result.Add(asValue.Value<string>());
                return result;
            }
            var asArray = scopes as JArray;
            if (asArray != null)
            {
                foreach (string s in asArray)
                {
                    result.Add(s);
                }
            }
            return result;
        }

        public static HttpMessageHandler GetBackChannelHandler(bool validateCertificates)
        {

            if (Platform.IsFullFramework)
            {
                return null;
            }
            else
            {
                if (!validateCertificates)
                {
                    var handler = new HttpClientHandler();
                    handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                    return handler;
                }
                return null;
            }
        }

    }
}


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

using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Steeltoe.Security.Authentication.MtlsCore
{
    public static class X509Certificate2Extensions
    {
        public static string SHA256Thumprint(this X509Certificate2 certificate)
        {
            using (var hasher = SHA256.Create())
            {
                var certificateHash = hasher.ComputeHash(certificate.RawData);
                string hashAsString = string.Empty;
                foreach (byte hashByte in certificateHash)
                {
                    hashAsString += hashByte.ToString("x2", CultureInfo.InvariantCulture);
                }

                return hashAsString;
            }
        }
    }
}

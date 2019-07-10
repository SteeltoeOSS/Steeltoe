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

using System;

namespace Steeltoe.Common.Extensions
{
    public static class UriExtensions
    {
        public static string ToMaskedString(this Uri source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.ToMaskedUri().ToString();
        }

        public static Uri ToMaskedUri(this Uri source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (string.IsNullOrEmpty(source.UserInfo))
            {
                return source;
            }

            var builder = new UriBuilder(source)
            {
                UserName = "****",
#pragma warning disable S2068 // Credentials should not be hard-coded
                Password = "****"
#pragma warning restore S2068 // Credentials should not be hard-coded
            };

            return builder.Uri;
        }
    }
}

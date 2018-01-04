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

namespace Steeltoe.Security.DataProtection.CredHub
{
    /// <summary>
    /// Parameters for generating a new password credential. All parameters are optional
    /// </summary>
    public class PasswordGenerationParameters
    {
        /// <summary>
        /// Gets or sets length of generated password value
        /// </summary>
        public int? Length { get; set; }

        /// <summary>
        /// Gets or sets exclude upper case alpha characters from generated credential value
        /// </summary>
        public bool? ExcludeUpper { get; set; }

        /// <summary>
        /// Gets or sets exclude lower case alpha characters from generated credential value
        /// </summary>
        public bool? ExcludeLower { get; set; }

        /// <summary>
        /// Gets or sets exclude numeric characters from generated credential value
        /// </summary>
        public bool? ExcludeNumber { get; set; }

        /// <summary>
        /// Gets or sets include non-alphanumeric characters in generated credential value
        /// </summary>
        public bool? IncludeSpecial { get; set; }
    }
}
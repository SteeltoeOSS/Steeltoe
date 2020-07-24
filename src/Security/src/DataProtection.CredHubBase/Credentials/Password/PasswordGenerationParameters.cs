// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

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
        [JsonPropertyName("exclude_upper")]
        public bool? ExcludeUpper { get; set; }

        /// <summary>
        /// Gets or sets exclude lower case alpha characters from generated credential value
        /// </summary>
        [JsonPropertyName("exclude_lower")]
        public bool? ExcludeLower { get; set; }

        /// <summary>
        /// Gets or sets exclude numeric characters from generated credential value
        /// </summary>
        [JsonPropertyName("exclude_number")]
        public bool? ExcludeNumber { get; set; }

        /// <summary>
        /// Gets or sets include non-alphanumeric characters in generated credential value
        /// </summary>
        [JsonPropertyName("include_special")]
        public bool? IncludeSpecial { get; set; }
    }
}
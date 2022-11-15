// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Security.DataProtection.CredHub.Credentials.Password;

namespace Steeltoe.Security.DataProtection.CredHub.Credentials.User;

public class UserGenerationParameters : PasswordGenerationParameters
{
    /// <summary>
    /// Gets or sets user provided value for username.
    /// </summary>
    public string Username { get; set; }
}

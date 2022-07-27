// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Security.DataProtection.CredHub;

public class UserSetRequest : CredentialSetRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserSetRequest"/> class.
    /// </summary>
    /// <param name="credentialName">Name of credential</param>
    /// <param name="userName">Name of the user</param>
    /// <param name="password">Password of the user</param>
    public UserSetRequest(string credentialName, string userName, string password)
    {
        Name = credentialName;
        Type = CredentialType.User;
        Value = new UserCredential { Username = userName, Password = password };
    }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Security.DataProtection.CredHub;

public class UserGenerationRequest : CredHubGenerateRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserGenerationRequest"/> class.
    /// Use to request a new <see cref="UserCredential"/>
    /// </summary>
    /// <param name="credentialName">Name of credential</param>
    /// <param name="parameters">Variables for username and password generation</param>
    /// <param name="overwriteMode">Overwrite existing credential (default: no-overwrite)</param>
    public UserGenerationRequest(string credentialName, UserGenerationParameters parameters, OverwiteMode overwriteMode = OverwiteMode.converge)
    {
        Name = credentialName;
        Type = CredentialType.User;
        Parameters = parameters;
        Mode = overwriteMode;
    }
}
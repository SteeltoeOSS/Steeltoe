// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Security.DataProtection.CredHub;

public class PasswordGenerationRequest : CredHubGenerateRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PasswordGenerationRequest" /> class. Use to Request a new Password.
    /// </summary>
    /// <param name="credentialName">
    /// Name of the credential.
    /// </param>
    /// <param name="parameters">
    /// Variables for password generation.
    /// </param>
    /// <param name="overwriteMode">
    /// Overwrite existing credential (default: no-overwrite).
    /// </param>
    public PasswordGenerationRequest(string credentialName, PasswordGenerationParameters parameters, OverwriteMode overwriteMode = OverwriteMode.Converge)
    {
        Name = credentialName;
        Type = CredentialType.Password;
        Parameters = parameters;
        Mode = overwriteMode;
    }
}

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Security.DataProtection.CredHub;

[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public class ValueSetRequest : CredentialSetRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValueSetRequest"/> class.
    /// </summary>
    /// <param name="credentialName">Name of credential</param>
    /// <param name="value">Value of the credential to set</param>
    public ValueSetRequest(string credentialName, string value)
    {
        Name = credentialName;
        Type = CredentialType.Value;
        Value = new ValueCredential(value);
    }
}
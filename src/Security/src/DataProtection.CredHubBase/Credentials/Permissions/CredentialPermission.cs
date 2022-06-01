// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Security.DataProtection.CredHub;

public class CredentialPermission
{
    /// <summary>
    /// Gets or sets schemed string identifying an actor -- auth_type:scope/primary_identifier
    /// </summary>
    public string Actor { get; set; }

    /// <summary>
    /// Gets or sets list of operations permissioned for the actor
    /// </summary>
    public List<OperationPermissions> Operations { get; set; }
}

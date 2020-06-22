﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Security.DataProtection.CredHub
{
    /// <summary>
    /// Used internally to process a list of paths from CredHub
    /// </summary>
    internal class CredentialPathsResponse
    {
        /// <summary>
        /// Gets or sets list of paths containing credentials
        /// </summary>
        public List<CredentialPath> Paths { get; set; }
    }
}
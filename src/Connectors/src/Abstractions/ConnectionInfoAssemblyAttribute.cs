﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Attributes;

namespace Steeltoe.Connector
{
    /// <summary>
    /// Identifies an assembly that contains one or more <see cref="IConnectionInfo" />
    /// </summary>
    public sealed class ConnectionInfoAssemblyAttribute : AssemblyContainsTypeAttribute
    {
        public ConnectionInfoAssemblyAttribute()
            : base(typeof(IConnectionInfo))
        {
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace Steeltoe.Security.DataProtection.CredHub
{
    [Serializable]
    public class CredHubException : Exception
    {
        public CredHubException()
        {
        }

        public CredHubException(string message)
            : base(message)
        {
        }

        public CredHubException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected CredHubException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}

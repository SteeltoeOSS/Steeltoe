// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace Steeltoe.CloudFoundry.Connector
{
    [Serializable]
    public class ConnectorException : Exception
    {
        public ConnectorException()
        {
        }

        public ConnectorException(string message)
            : base(message)
        {
        }

        public ConnectorException(string message, Exception nested)
            : base(message, nested)
        {
        }

        protected ConnectorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}

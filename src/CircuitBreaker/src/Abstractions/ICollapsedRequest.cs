// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.CircuitBreaker
{
#pragma warning disable S3246 // Generic type parameters should be co/contravariant when possible
    public interface ICollapsedRequest<RequestResponseType, RequestArgumentType>
#pragma warning restore S3246 // Generic type parameters should be co/contravariant when possible
    {
        RequestArgumentType Argument { get; }

        RequestResponseType Response { get; set; }

        Exception Exception { get; set; }

        bool Complete { get; set; }
    }
}

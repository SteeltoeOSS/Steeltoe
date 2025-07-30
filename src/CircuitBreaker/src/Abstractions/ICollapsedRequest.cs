// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.CircuitBreaker;
#pragma warning disable S3246 // Generic type parameters should be co/contravariant when possible
[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public interface ICollapsedRequest<RequestResponseType, RequestArgumentType>
#pragma warning restore S3246 // Generic type parameters should be co/contravariant when possible
{
    RequestArgumentType Argument { get; }

    RequestResponseType Response { get; set; }

    Exception Exception { get; set; }

    bool Complete { get; set; }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using System.Collections.Generic;

namespace Steeltoe.Integration.Mapping;

[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public interface IRequestReplyHeaderMapper<T>
{
    void FromHeadersToRequest(IMessageHeaders headers, T target);

    void FromHeadersToReply(IMessageHeaders headers, T target);

    IDictionary<string, object> ToHeadersFromRequest(T source);

    IDictionary<string, object> ToHeadersFromReply(T source);
}
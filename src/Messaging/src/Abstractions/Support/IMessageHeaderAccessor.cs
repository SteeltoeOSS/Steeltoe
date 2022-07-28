// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Support;

public interface IMessageHeaderAccessor
{
    string Id { get; }

    long? Timestamp { get; }

    string ContentType { get; set; }

    bool LeaveMutable { get; set; }

    bool IsMutable { get; }

    bool IsModified { get; set; }

    IMessageHeaders MessageHeaders { get; }

    string ReplyChannelName { get; set; }

    object ReplyChannel { get; set; }

    string ErrorChannelName { get; set; }

    object ErrorChannel { get; set; }

    bool EnableTimestamp { get; set; }

    IIdGenerator IdGenerator { get; set; }

    void SetImmutable();

    IMessageHeaders ToMessageHeaders();

    IDictionary<string, object> ToDictionary();

    object GetHeader(string headerName);

    void SetHeader(string name, object value);

    void SetHeaderIfAbsent(string name, object value);

    void RemoveHeader(string headerName);

    void RemoveHeaders(params string[] headerPatterns);

    void CopyHeaders(IDictionary<string, object> headersToCopy);

    void CopyHeadersIfAbsent(IDictionary<string, object> headersToCopy);
}

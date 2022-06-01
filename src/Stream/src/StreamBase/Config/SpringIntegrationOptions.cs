// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Stream.Config;

public class SpringIntegrationOptions
{
    public const string PREFIX = "spring:cloud:stream:integration";
    private static readonly string[] _messageHandlerNotPropagatedHeaders = new string[] { MessageHeaders.CONTENT_TYPE };

    public string[] MessageHandlerNotPropagatedHeaders { get; set; } = _messageHandlerNotPropagatedHeaders;
}

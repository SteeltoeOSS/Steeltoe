// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Stream.Configuration;

public class SpringIntegrationOptions
{
    public const string Prefix = "spring:cloud:stream:integration";

    private static readonly string[] DefaultMessageHandlerNotPropagatedHeaders =
    {
        MessageHeaders.ContentType
    };

    public string[] MessageHandlerNotPropagatedHeaders { get; set; } = DefaultMessageHandlerNotPropagatedHeaders;
}

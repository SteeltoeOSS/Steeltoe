// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Integration;
using Steeltoe.Messaging;

namespace Steeltoe.Stream.Binder;

public static class BinderHeaders
{
    private const string Prefix = "scst_";
    public const string BinderOriginalContentType = "originalContentType";

    public const string PartitionHeader = $"{Prefix}partition";

    public const string PartitionOverride = $"{Prefix}partitionOverride";

    public const string NativeHeadersPresent = $"{Prefix}nativeHeadersPresent";

    public static readonly string[] StandardHeaders =
    {
        IntegrationMessageHeaderAccessor.CorrelationId,
        IntegrationMessageHeaderAccessor.SequenceSize,
        IntegrationMessageHeaderAccessor.SequenceNumber,
        MessageHeaders.ContentType,
        BinderOriginalContentType
    };
}

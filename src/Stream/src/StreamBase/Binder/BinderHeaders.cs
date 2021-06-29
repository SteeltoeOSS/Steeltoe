// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Integration;
using Steeltoe.Messaging;

namespace Steeltoe.Stream.Binder
{
    public static class BinderHeaders
    {
        public const string BINDER_ORIGINAL_CONTENT_TYPE = "originalContentType";

        public const string PARTITION_HEADER = PREFIX + "partition";

        public const string PARTITION_OVERRIDE = PREFIX + "partitionOverride";

        public const string NATIVE_HEADERS_PRESENT = PREFIX + "nativeHeadersPresent";

        public static readonly string[] STANDARD_HEADERS = new string[]
        {
            IntegrationMessageHeaderAccessor.CORRELATION_ID,
            IntegrationMessageHeaderAccessor.SEQUENCE_SIZE,
            IntegrationMessageHeaderAccessor.SEQUENCE_NUMBER,
            MessageHeaders.CONTENT_TYPE,
            BINDER_ORIGINAL_CONTENT_TYPE
        };

        private const string PREFIX = "scst_";
    }
}

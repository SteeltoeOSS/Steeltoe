// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IO.Compression;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Steeltoe.Messaging.RabbitMQ.Support.PostProcessor;

public abstract class AbstractDeflaterPostProcessor : AbstractCompressingPostProcessor
{
    public virtual CompressionLevel Level { get; set; } = CompressionLevel.Fastest;

    protected AbstractDeflaterPostProcessor()
        : base(new LoggerFactory())
    {
    }

    protected AbstractDeflaterPostProcessor(bool autoDecompress)
        : base(autoDecompress, new LoggerFactory())
    {
    }
}

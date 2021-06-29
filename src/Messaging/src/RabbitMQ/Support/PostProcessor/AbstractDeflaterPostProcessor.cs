// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;

namespace Steeltoe.Messaging.RabbitMQ.Support.PostProcessor
{
    public abstract class AbstractDeflaterPostProcessor : AbstractCompressingPostProcessor
    {
        protected AbstractDeflaterPostProcessor()
        {
        }

        protected AbstractDeflaterPostProcessor(bool autoDecompress)
        : base(autoDecompress)
        {
        }

        public virtual CompressionLevel Level { get; set; } = CompressionLevel.Fastest;
    }
}

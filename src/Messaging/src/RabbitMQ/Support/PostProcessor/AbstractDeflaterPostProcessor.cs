using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;

namespace Steeltoe.Messaging.Rabbit.Support.PostProcessor
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

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Steeltoe.Messaging.Rabbit.Support.PostProcessor
{
    class InflaterPostProcessor : AbstractDecompressingPostProcessor
    {
        public InflaterPostProcessor()
        {
        }

        public InflaterPostProcessor(bool autoDecompress)
        : base(autoDecompress)
        {
        }

        protected override Stream GetDeCompressorStream(Stream stream)
        {
            return new DeflateStream(stream, CompressionMode.Decompress);
        }

        protected override string GetEncoding()
        {
            return "deflate";
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Steeltoe.Messaging.Rabbit.Support.PostProcessor
{
    public class GUnzipPostProcessor : AbstractDecompressingPostProcessor
    {

        public GUnzipPostProcessor()
        {
        }

        public GUnzipPostProcessor(bool alwaysDecompress)
        : base(alwaysDecompress)
        {
        }

        protected override Stream GetDeCompressorStream(Stream zipped)
        {
            return new GZipStream(zipped, CompressionMode.Decompress);
        }

        protected override string GetEncoding()
        {
            return "gzip";
        }
    }
}

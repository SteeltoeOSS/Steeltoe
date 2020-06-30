using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Steeltoe.Messaging.Rabbit.Support.PostProcessor
{
    public class DeflaterPostProcessor : AbstractDeflaterPostProcessor
    {
        public DeflaterPostProcessor()
        {
        }

        public DeflaterPostProcessor(bool autoDecompress)
        : base(autoDecompress)
        {
        }

        protected override Stream GetCompressorStream(Stream stream)
        {
            return new DeflateStream(stream, Level);
        }

        protected override string GetEncoding()
        {
            return "deflate";
        }
    }
}

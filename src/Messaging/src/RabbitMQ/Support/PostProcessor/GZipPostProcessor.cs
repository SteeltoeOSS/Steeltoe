using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Steeltoe.Messaging.Rabbit.Support.PostProcessor
{
    public class GZipPostProcessor : AbstractDeflaterPostProcessor
    {

        public GZipPostProcessor()
        {
        }

        public GZipPostProcessor(bool autoDecompress)
        : base(autoDecompress)
        {
        }

        protected override Stream GetCompressorStream(Stream zipped)
        {
            return new GZipStream(zipped, Level);
        }

        protected override string GetEncoding()
        {
            return "gzip";
        }
    }
}

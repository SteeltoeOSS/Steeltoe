using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Steeltoe.Messaging.Rabbit.Support.PostProcessor
{
    public class UnzipPostProcessor : AbstractDecompressingPostProcessor
    {
        public UnzipPostProcessor()
        {
        }

        public UnzipPostProcessor(bool alwaysDecompress)
        : base(alwaysDecompress)
        {
        }


        protected override Stream GetDeCompressorStream(Stream zipped)
        {
            var zipper = new ZipArchive(zipped, ZipArchiveMode.Read);
            var entry = zipper.GetEntry("amqp");
            if (entry == null)
            {
                throw new InvalidOperationException("Zip entryName 'amqp' does not exist");
            }

            return entry.Open();
        }


        protected override string GetEncoding()
        {
            return "zip";
        }
    }
}

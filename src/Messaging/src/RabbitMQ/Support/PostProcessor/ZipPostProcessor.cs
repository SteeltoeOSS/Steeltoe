using Microsoft.Extensions.Logging;
using Steeltoe.Messaging.Rabbit.Exceptions;
using Steeltoe.Messaging.Rabbit.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Steeltoe.Messaging.Rabbit.Support.PostProcessor
{
    public class ZipPostProcessor : AbstractDeflaterPostProcessor
    {
        public ZipPostProcessor()
        {
        }

        public ZipPostProcessor(bool autoDecompress)
        : base(autoDecompress)
        {
        }
        public override IMessage PostProcessMessage(IMessage message)
        {
            try
            {
                var zipped = new MemoryStream();
                var zipper = new ZipArchive(zipped, ZipArchiveMode.Create);
                var entry = zipper.CreateEntry("amqp", Level);
                var compressor = entry.Open();
                var payStream = new MemoryStream((byte[])message.Payload);
                payStream.CopyTo(compressor);
                compressor.Close();
                zipper.Dispose();

                byte[] compressed = zipped.ToArray();

                _logger?.LogTrace("Compressed " + ((byte[])message.Payload).Length + " to " + compressed.Length);

                return CreateMessage(message, compressed);
            }
            catch (IOException e)
            {
                throw new RabbitIOException(e);
            }
        }

        protected override Stream GetCompressorStream(Stream zipped)
        {
            throw new NotImplementedException("GetCompressorStream should not be called");
        }

        protected override string GetEncoding()
        {
            return "zip";
        }
    }
}

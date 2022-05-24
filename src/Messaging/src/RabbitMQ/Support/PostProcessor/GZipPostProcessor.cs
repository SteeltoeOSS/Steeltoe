// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.IO.Compression;

namespace Steeltoe.Messaging.RabbitMQ.Support.PostProcessor
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

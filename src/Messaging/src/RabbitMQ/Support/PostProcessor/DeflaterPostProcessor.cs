// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Steeltoe.Messaging.RabbitMQ.Support.PostProcessor
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

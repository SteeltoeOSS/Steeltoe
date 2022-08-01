// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IO.Compression;

namespace Steeltoe.Messaging.RabbitMQ.Support.PostProcessor;

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

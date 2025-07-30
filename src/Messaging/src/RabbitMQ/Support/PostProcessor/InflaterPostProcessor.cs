// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Steeltoe.Messaging.RabbitMQ.Support.PostProcessor;

[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public class InflaterPostProcessor : AbstractDecompressingPostProcessor
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
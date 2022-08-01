// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.IO.Compression;

namespace Steeltoe.Messaging.RabbitMQ.Support.PostProcessor;

public class UnzipPostProcessor : AbstractDecompressingPostProcessor
{
    public UnzipPostProcessor()
    {
    }

    public UnzipPostProcessor(bool alwaysDecompress)
        : base(alwaysDecompress)
    {
    }

    protected override Stream GetDeCompressorStream(Stream stream)
    {
        var zipper = new ZipArchive(stream, ZipArchiveMode.Read);
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

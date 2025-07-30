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